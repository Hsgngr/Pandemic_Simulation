import pytest

import numpy as np

from mlagents.trainers.ghost.trainer import GhostTrainer
from mlagents.trainers.ghost.controller import GhostController
from mlagents.trainers.behavior_id_utils import BehaviorIdentifiers
from mlagents.trainers.ppo.trainer import PPOTrainer
from mlagents.trainers.brain import BrainParameters
from mlagents.trainers.agent_processor import AgentManagerQueue
from mlagents.trainers.tests import mock_brain as mb
from mlagents.trainers.tests.test_trajectory import make_fake_trajectory
from mlagents.trainers.settings import TrainerSettings, SelfPlaySettings


@pytest.fixture
def dummy_config():
    return TrainerSettings(self_play=SelfPlaySettings())


VECTOR_ACTION_SPACE = [1]
VECTOR_OBS_SPACE = 8
DISCRETE_ACTION_SPACE = [3, 3, 3, 2]
BUFFER_INIT_SAMPLES = 513
NUM_AGENTS = 12


@pytest.mark.parametrize("use_discrete", [True, False])
def test_load_and_set(dummy_config, use_discrete):
    mock_brain = mb.setup_mock_brain(
        use_discrete,
        False,
        vector_action_space=VECTOR_ACTION_SPACE,
        vector_obs_space=VECTOR_OBS_SPACE,
        discrete_action_space=DISCRETE_ACTION_SPACE,
    )

    trainer_params = dummy_config
    trainer = PPOTrainer(mock_brain.brain_name, 0, trainer_params, True, False, 0, "0")
    trainer.seed = 1
    policy = trainer.create_policy(mock_brain.brain_name, mock_brain)
    policy.create_tf_graph()
    trainer.seed = 20  # otherwise graphs are the same
    to_load_policy = trainer.create_policy(mock_brain.brain_name, mock_brain)
    to_load_policy.create_tf_graph()
    to_load_policy.init_load_weights()

    weights = policy.get_weights()
    load_weights = to_load_policy.get_weights()
    try:
        for w, lw in zip(weights, load_weights):
            np.testing.assert_array_equal(w, lw)
    except AssertionError:
        pass

    to_load_policy.load_weights(weights)
    load_weights = to_load_policy.get_weights()

    for w, lw in zip(weights, load_weights):
        np.testing.assert_array_equal(w, lw)


def test_process_trajectory(dummy_config):
    brain_params_team0 = BrainParameters(
        brain_name="test_brain?team=0",
        vector_observation_space_size=1,
        camera_resolutions=[],
        vector_action_space_size=[2],
        vector_action_descriptions=[],
        vector_action_space_type=0,
    )

    brain_name = BehaviorIdentifiers.from_name_behavior_id(
        brain_params_team0.brain_name
    ).brain_name

    brain_params_team1 = BrainParameters(
        brain_name="test_brain?team=1",
        vector_observation_space_size=1,
        camera_resolutions=[],
        vector_action_space_size=[2],
        vector_action_descriptions=[],
        vector_action_space_type=0,
    )
    ppo_trainer = PPOTrainer(brain_name, 0, dummy_config, True, False, 0, "0")
    controller = GhostController(100)
    trainer = GhostTrainer(
        ppo_trainer, brain_name, controller, 0, dummy_config, True, "0"
    )

    # first policy encountered becomes policy trained by wrapped PPO
    parsed_behavior_id0 = BehaviorIdentifiers.from_name_behavior_id(
        brain_params_team0.brain_name
    )
    policy = trainer.create_policy(parsed_behavior_id0, brain_params_team0)
    trainer.add_policy(parsed_behavior_id0, policy)
    trajectory_queue0 = AgentManagerQueue(brain_params_team0.brain_name)
    trainer.subscribe_trajectory_queue(trajectory_queue0)

    # Ghost trainer should ignore this queue because off policy
    parsed_behavior_id1 = BehaviorIdentifiers.from_name_behavior_id(
        brain_params_team1.brain_name
    )
    policy = trainer.create_policy(parsed_behavior_id1, brain_params_team1)
    trainer.add_policy(parsed_behavior_id1, policy)
    trajectory_queue1 = AgentManagerQueue(brain_params_team1.brain_name)
    trainer.subscribe_trajectory_queue(trajectory_queue1)

    time_horizon = 15
    trajectory = make_fake_trajectory(
        length=time_horizon,
        max_step_complete=True,
        vec_obs_size=1,
        num_vis_obs=0,
        action_space=[2],
    )
    trajectory_queue0.put(trajectory)
    trainer.advance()

    # Check that trainer put trajectory in update buffer
    assert trainer.trainer.update_buffer.num_experiences == 15

    trajectory_queue1.put(trajectory)
    trainer.advance()

    # Check that ghost trainer ignored off policy queue
    assert trainer.trainer.update_buffer.num_experiences == 15
    # Check that it emptied the queue
    assert trajectory_queue1.empty()


def test_publish_queue(dummy_config):
    brain_params_team0 = BrainParameters(
        brain_name="test_brain?team=0",
        vector_observation_space_size=8,
        camera_resolutions=[],
        vector_action_space_size=[1],
        vector_action_descriptions=[],
        vector_action_space_type=0,
    )

    parsed_behavior_id0 = BehaviorIdentifiers.from_name_behavior_id(
        brain_params_team0.brain_name
    )

    brain_name = parsed_behavior_id0.brain_name

    brain_params_team1 = BrainParameters(
        brain_name="test_brain?team=1",
        vector_observation_space_size=8,
        camera_resolutions=[],
        vector_action_space_size=[1],
        vector_action_descriptions=[],
        vector_action_space_type=0,
    )
    ppo_trainer = PPOTrainer(brain_name, 0, dummy_config, True, False, 0, "0")
    controller = GhostController(100)
    trainer = GhostTrainer(
        ppo_trainer, brain_name, controller, 0, dummy_config, True, "0"
    )

    # First policy encountered becomes policy trained by wrapped PPO
    # This queue should remain empty after swap snapshot
    policy = trainer.create_policy(parsed_behavior_id0, brain_params_team0)
    trainer.add_policy(parsed_behavior_id0, policy)
    policy_queue0 = AgentManagerQueue(brain_params_team0.brain_name)
    trainer.publish_policy_queue(policy_queue0)

    # Ghost trainer should use this queue for ghost policy swap
    parsed_behavior_id1 = BehaviorIdentifiers.from_name_behavior_id(
        brain_params_team1.brain_name
    )
    policy = trainer.create_policy(parsed_behavior_id1, brain_params_team1)
    trainer.add_policy(parsed_behavior_id1, policy)
    policy_queue1 = AgentManagerQueue(brain_params_team1.brain_name)
    trainer.publish_policy_queue(policy_queue1)

    # check ghost trainer swap pushes to ghost queue and not trainer
    assert policy_queue0.empty() and policy_queue1.empty()
    trainer._swap_snapshots()
    assert policy_queue0.empty() and not policy_queue1.empty()
    # clear
    policy_queue1.get_nowait()

    mock_brain = mb.setup_mock_brain(
        False,
        False,
        vector_action_space=VECTOR_ACTION_SPACE,
        vector_obs_space=VECTOR_OBS_SPACE,
        discrete_action_space=DISCRETE_ACTION_SPACE,
    )

    buffer = mb.simulate_rollout(BUFFER_INIT_SAMPLES, mock_brain)
    # Mock out reward signal eval
    buffer["extrinsic_rewards"] = buffer["environment_rewards"]
    buffer["extrinsic_returns"] = buffer["environment_rewards"]
    buffer["extrinsic_value_estimates"] = buffer["environment_rewards"]
    buffer["curiosity_rewards"] = buffer["environment_rewards"]
    buffer["curiosity_returns"] = buffer["environment_rewards"]
    buffer["curiosity_value_estimates"] = buffer["environment_rewards"]
    buffer["advantages"] = buffer["environment_rewards"]
    trainer.trainer.update_buffer = buffer

    # when ghost trainer advance and wrapped trainer buffers full
    # the wrapped trainer pushes updated policy to correct queue
    assert policy_queue0.empty() and policy_queue1.empty()
    trainer.advance()
    assert not policy_queue0.empty() and policy_queue1.empty()


if __name__ == "__main__":
    pytest.main()
