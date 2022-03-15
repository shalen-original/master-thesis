# HoloAssist Apps

This repository contains a collection of applications to be used with the HoloAssist Hololens app.

The documentation on how to write a HoloAssist App is available [in the docs folder](/docs).

## Running HoloAssist Apps

In order to run these applications, you will need Python3 (tested with Python 3.8.3). After cloning the repository, initialize a virtual environment with the required dependencies:

```sh
cd holo-assist-apps
python -m venv .venv

#Enable the virtual environment (depends on the OS)
#On Powershell it is something like:
& .\.venv\Scripts\Activate.ps1

# Install the dependencies
pip install -r requirements.txt
```

After this setup, you can run the individual applications. You can target them either at a locally running Unity instance on which the HoloAssist project is running (for development) or at a real Hololens with HoloAssist running.

To start an app and have it send data to a local Unity instance:

```sh
python src/munich_runway.py --unity
```

To start an app and have it send data to the Hololens:

```sh
python src/munich_runway.py
```

Please consider that the Hololens IP address at the moment is hardcoded in `src\lib\__init__.py`, if needed remember to change it. After starting HoloAssist, the Hololens must be brought close to the simulator's QR code until it has had enough time to scan them and correctly estimate their position.