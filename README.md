## MLAgents UR10

This project shows a UR10 robot manipulator that learned the reaching task using Unity MLAgents 1.9 and Unity version 2021.1.6f1 for Ubuntu 18.04.

The UR10 description files are from [this repo](https://github.com/ros-industrial/universal_robot.git).

The robotiq 85 gripper files are from [this repo](https://github.com/beta-robots/robotiq.git)

## Using this Repository

* Clone this repository with ```git clone```.
* Import project into Unity.
* Press play. The robot should move towards the target. It is possible to modify the target position on the fly so that the robot moves to the new target goal.

[![Example](https://j.gifs.com/vQJnV8.gif)]

## Notes
* The training took approximately 4:30 hours on a single RTX2070 using a farm with 16 instances.

