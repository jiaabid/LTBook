<!-- tool: Spectroscopic Ellipsometer -->
# Spectroscopic Ellipsometer: Quick Operating Guide

> **Fictional sample content.** This guide was written for the ToolBook demo. It is not a real instrument manual. Do not use it to operate real equipment.

## Overview

The ellipsometer measures the change in polarisation of light reflected from a sample over 190–1700 nm. From this it determines thin-film thickness and optical constants (n and k). Typical films are 1 nm to 10 µm thick.

## Start-up

1. Switch on the lamp at least 20 minutes before measuring, so the intensity stabilises.
2. Start the acquisition software and log in with your lab account.
3. Run the **System Check** from the Hardware menu. It must report "Pass" before you continue.

## Calibration

Calibrate at the start of every session using the reference silicon wafer with 25 nm thermal oxide, which is kept in the labelled box next to the instrument.

1. Place the reference wafer on the stage and switch on the vacuum chuck.
2. Run **Calibrate → Standard Calibration**.
3. Accept the calibration only if the fitted oxide thickness is within ±0.5 nm of the value printed on the box.

## Measuring a sample

1. Place the sample in the centre of the stage, film side up, and switch on the vacuum chuck. Samples smaller than 1 cm × 1 cm may not hold under vacuum. Use the small-sample adapter.
2. Use the alignment camera to adjust the stage height until the reflected spot is centred on the crosshair.
3. Choose angles of incidence. For most thin films, measure at 65°, 70° and 75°.
4. Start the measurement. A full spectroscopic scan at three angles takes about 8 minutes.

## Fitting the data

Build an optical model in the software: substrate, then each film layer. Start from the material library (for example "Si", "SiO2", "Cauchy" for transparent films). Fit thickness first, then optical constants. A good fit has a mean squared error (MSE) below 5. A high MSE usually means the model is missing a layer (such as native oxide or surface roughness).

## Shutting down

Remove your sample, switch off the vacuum chuck and leave the lamp on if another booking starts within two hours (lamp lifetime is reduced by frequent switching). Otherwise switch the lamp off. Check out on ToolBook.

## Troubleshooting

**The software reports low signal intensity.** The sample may be tilted or the stage height may be wrong. Realign using the alignment camera. Rough or strongly scattering samples give low intensity by nature.
