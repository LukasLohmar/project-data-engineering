# project-data-engineering

Imagine that you work for a municipality that has installed various sensors throughout the city to measure environmental metrics. The project's overall goal is to provide planners with better information to improve the city's environmental conditions in the long term. Also, the data will be used for developing an application that quickly warns citizens if measures exceed recommended values. The project started a couple of months ago, and so far, some of the sensors have been installed. They have already collected the first data comprising roughly half a million measurements. Your goal is to design and implement a data processing system that reliably stores the data making it accessible and usable by the front-end applications planned for this project.

## build and run
1. run ``docker build . -t data-system`` inside /src/data-system root to build the docker image
2. run ``docker compose up -d`` to start the project as a new docker container
    - ``SQL_SERVER`` environment variable needs to be set
    - if ``docker compose`` errors out with ``error getting credentials - err: exec: "docker-credential-desktop": executable file not found in %PATH%, out: ''`` -> change **credsStore** to **credStore** in ``%USERPROFILE%/.docker/config.json``
## run tests



Disclaimer: the initial [data set](https://www.kaggle.com/datasets/garystafford/environmental-sensor-data-132k) is under [CC0: Public Domain License](https://creativecommons.org/publicdomain/zero/1.0/)
