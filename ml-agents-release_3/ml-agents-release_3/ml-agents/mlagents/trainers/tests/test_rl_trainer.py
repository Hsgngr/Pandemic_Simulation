from unittest import mock
import pytest
import mlagents.trainers.tests.mock_brain as mb
from mlagents.trainers.trainer.rl_trainer import RLTrainer
from mlagents.trainers.tests.test_buffer import construct_fake_buffer
from mlagents.trainers.agent_processor import AgentManagerQueue
from mlagents.trainers.settings import TrainerSettings


def create_mock_brain():
    mock_brain = mb.create_mock_brainparams(
        vector_action_space_type="continuous",
        vector_action_space_size=[2],
        vector_observation_space_size=8,
        number_visual_observations=1,
    )
    return mock_brain


# Add concrete implementations of abstract methods
class FakeTrainer(RLTrainer):
    def set_is_policy_updating(self, is_updating):
        self.update_policy = is_updating

    def get_policy(self, name_behavior_id):
        return mock.Mock()

    def _is_ready_update(self):
        return True

    def _update_policy(self):
        return self.update_policy

    def add_policy(self):
        pass

    def create_policy(self):
        return mock.Mock()

    def _process_trajectory(self, trajectory):
        super()._process_trajectory(trajectory)


def create_rl_trainer():
    mock_brainparams = create_mock_brain()
    trainer = FakeTrainer(
        mock_brainparams,
        TrainerSettings(max_steps=100, checkpoint_interval=10, summary_freq=20),
        True,
        0,
    )
    trainer.set_is_policy_updating(True)
    return trainer


def test_rl_trainer():
    trainer = create_rl_trainer()
    agent_id = "0"
    trainer.collected_rewards["extrinsic"] = {agent_id: 3}
    # Test end episode
    trainer.end_episode()
    for rewards in trainer.collected_rewards.values():
        for agent_id in rewards:
            assert rewards[agent_id] == 0


def test_clear_update_buffer():
    trainer = create_rl_trainer()
    trainer.update_buffer = construct_fake_buffer(0)
    trainer._clear_update_buffer()
    for _, arr in trainer.update_buffer.items():
        assert len(arr) == 0


@mock.patch("mlagents.trainers.trainer.rl_trainer.RLTrainer._clear_update_buffer")
def test_advance(mocked_clear_update_buffer):
    trainer = create_rl_trainer()
    trajectory_queue = AgentManagerQueue("testbrain")
    policy_queue = AgentManagerQueue("testbrain")
    trainer.subscribe_trajectory_queue(trajectory_queue)
    trainer.publish_policy_queue(policy_queue)
    time_horizon = 10
    trajectory = mb.make_fake_trajectory(
        length=time_horizon,
        max_step_complete=True,
        vec_obs_size=1,
        num_vis_obs=0,
        action_space=[2],
    )
    trajectory_queue.put(trajectory)

    trainer.advance()
    policy_queue.get_nowait()
    # Check that get_step is correct
    assert trainer.get_step == time_horizon
    # Check that we can turn off the trainer and that the buffer is cleared
    for _ in range(0, 5):
        trajectory_queue.put(trajectory)
        trainer.advance()
        # Check that there is stuff in the policy queue
        policy_queue.get_nowait()

    # Check that if the policy doesn't update, we don't push it to the queue
    trainer.set_is_policy_updating(False)
    for _ in range(0, 10):
        trajectory_queue.put(trajectory)
        trainer.advance()
        # Check that there nothing  in the policy queue
        with pytest.raises(AgentManagerQueue.Empty):
            policy_queue.get_nowait()

    # Check that the buffer has been cleared
    assert not trainer.should_still_train
    assert mocked_clear_update_buffer.call_count > 0


@mock.patch("mlagents.trainers.trainer.trainer.Trainer.save_model")
@mock.patch("mlagents.trainers.trainer.trainer.StatsReporter.write_stats")
def test_summary_checkpoint(mock_write_summary, mock_save_model):
    trainer = create_rl_trainer()
    trajectory_queue = AgentManagerQueue("testbrain")
    policy_queue = AgentManagerQueue("testbrain")
    trainer.subscribe_trajectory_queue(trajectory_queue)
    trainer.publish_policy_queue(policy_queue)
    time_horizon = 10
    summary_freq = trainer.trainer_settings.summary_freq
    checkpoint_interval = trainer.trainer_settings.checkpoint_interval
    trajectory = mb.make_fake_trajectory(
        length=time_horizon,
        max_step_complete=True,
        vec_obs_size=1,
        num_vis_obs=0,
        action_space=[2],
    )
    # Check that we can turn off the trainer and that the buffer is cleared
    num_trajectories = 5
    for _ in range(0, num_trajectories):
        trajectory_queue.put(trajectory)
        trainer.advance()
        # Check that there is stuff in the policy queue
        policy_queue.get_nowait()

    # Check that we have called write_summary the appropriate number of times
    calls = [
        mock.call(step)
        for step in range(summary_freq, num_trajectories * time_horizon, summary_freq)
    ]
    mock_write_summary.assert_has_calls(calls, any_order=True)

    calls = [
        mock.call(trainer.brain_name)
        for step in range(
            checkpoint_interval, num_trajectories * time_horizon, checkpoint_interval
        )
    ]
    mock_save_model.assert_has_calls(calls, any_order=True)
