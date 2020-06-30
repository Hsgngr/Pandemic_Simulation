# Pandemic Simulation
Pandemic Simulation with Reinforcement Learning 

## Getting Started

This is an open source pandemic simulation environment for reinforcement learning created with Unity ml-agents. This is a dissertation project for Advanced Computer Science Master in University of Sussex.

To try out the project you should
*open the "Project" folder in Pandemic_Simulation\ml-agents-release_3\ml-agents-release_3\Project with Unity Editor.
* Play Assets/PandemicSimulation/Scenes/T1.unity


### Prerequisites
This project uses Unity-ml-agent 1.1.0 Release 3
Unity 2919.4.0f1
Anaconda Environment with Python 3.7
Tensorflow 2.2.0
```
Give examples
```

### Installing

A step by step series of examples that tell you how to get a development env running

To train an agent:
*Go to the folder of training.yaml
*Activate the environment
*
```
mlagents-learn ./trainer_config.yaml --run-id first_run
```

To see the results use

```
tensorboard --logdir results
```

### Screenshots from the simulation:
Simulating how infection spreads

![](images/pandemic_simulation.png)

States of agents: Represents the SIR Model which is used for visualizing pandemic disease.

![](images/agent_states.png)

## Authors

* **Ege Hosgungor** - *Initial work* - [Hsgngr](https://github.com/Hsgngr)

See also the list of [contributors](https://github.com/your/project/contributors) who participated in this project.


## Acknowledgments

### Inspiration
This project is inspired from followings:
#### Coronavirus Related
* https://www.washingtonpost.com/graphics/2020/world/corona-simulator/
* https://www.youtube.com/watch?v=gxAaO2rsdIs

#### Reinforcement Learning Related
* https://connect.unity.com/p/ml-agents-penguins-unity-learn
* https://learn.unity.com/course/ml-agents-hummingbirds?uv=2019.3



