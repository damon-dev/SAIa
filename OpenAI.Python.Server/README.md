## Dependencies

```bash
### Install Python + Dependencies
sudo -H apt-get install -y python-dev
sudo -H apt-get install -y python-pip
sudo -H apt-get install -y python-numpy python-dev cmake zlib1g-dev libjpeg-dev xvfb xorg-dev python-opengl libboost-all-dev libsdl2-dev swig

sudo -H pip install werkzeug
sudo -H pip install itsdangerous
sudo -H pip install click

# Export our display settings for XMing
export DISPLAY=localhost:0.0
echo 'export DISPLAY=localhost:0.0 ' >> ~/.bashrc
```

## Install

```bash
sudo -H pip install -r requirements.txt
```

## Run

```bash
python gym_http_server.py
```