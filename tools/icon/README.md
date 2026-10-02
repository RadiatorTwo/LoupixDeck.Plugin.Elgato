# Plugin icon generator

`make_icon.py` draws the plugin icon: a glowing LED panel light on a pole, matte, night blue.
It follows the Audio and CoolerControl plugin icons (same background, colors and shading) and is
original artwork, no third-party source. The Elgato logo and product artwork are not used.

```bash
pip install pillow numpy
python tools/icon/make_icon.py tools/icon/out
cp tools/icon/out/icon_256.png icon.png
```

The script writes `icon_{256,128,64,32,16}.png` into the given folder; only the 256 px file is
used, as `icon.png` in the repo root. The `out/` folder is not committed.
