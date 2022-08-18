# ProPresenter Lighting Controller

## Scenes
Scenes are used to store lighting configurations. 
```yml
# List of lighting scenes
scenes:
- name: Worship
  transition_time: 
  midi_note:
    channel: 2
    note: 0
```

### Scene Inputs
Each scene has a list of inputs that tell the light fixtures what color to be. Feel free to copy the following snippets into the config and remove the lines starting with a "#" if desired. Each input type has at least the following structure:
```yml
  - !type
    # Comma seperated list of fixture ids assigned to this input
    # Order may matter depending on the input type
    fixture_ids: 1,2,4-5
    # Maximum intensity as a percent or "auto" to calculate the intensity from amount of black in the color
    intensity: 40%
```

`!color_input` allows you to specify a color in RGB format.
Use an [online color picker](https://www.google.com/search?q=color+picker) to get the values.
```yml
  - !color_input
    # RGB color with each value between 0 and 255
    rgb:
      red: 255
      green: 255
      blue: 255
    fixture_ids: 1,2,4
    intensity: 40%
```

`!propresenter_input` uses the media that is showing on screen to generate the colors. See the ProPresenter section for more information.
```yml
  - !propresenter_input
    # True if you want the lights to change color while the media is playing, otherwise false
    has_motion: false
    # Minimum desired intensity
    min_intensity: 60%
    # Saturation where 100% is full color and 0% is white
    saturation: 100%
    fixture_ids: 7-13,6,14-20
    intensity: 90%
```

`!rainbow_input` cycles through all colors at the specified rate.
```yml
  - !rainbow_input
    # Saturation where 100% is full color and 0% is white
    saturation: 70%
    # The length of the color cycle in seconds
    cycle_length: 3
    fixture_ids: 6-19
    intensity: 90%
```

`!debug_input` creates a color picker below the scene list.
```yml
  - !debug_input
    fixture_ids: 6-20
    intensity: 100%
```