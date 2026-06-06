# Description

This is a frontend for using REAPER in a live session environment. It connects to REAPER over OSC. It is a pure web application that serves as the "dashboard" during playback and for controlling the session.

# Features

The dashboard is arranged in areas.
1. At the bottom there is a grid showing the 4x2 "FX parameter matrix" that is currently already implemented by the webserver. This grid should take about 3/4 of the horizontal space. The vertical height is 1/3 of the estate.
2. At the bottom right (the last 1/4 of the screen estate) shows the "Transport" display. This is also 1/3 of the screen height.
3. The top we have the "track view". This is 2/3 in height and 3/4 in width.
4. The top right there is the "event view". This is 2/3 in height and 1/4 in width.

# Components

## FX Parameter Matrix

The FX paramter matrix displays a maximum of 4 paramters of the currently selected track. This is the component that is already implemented by the web server right now and should mostly stay as is, just move it to a component and arrange it into the dashboard.

## Transport

The transport displays the current state of the transport (play, stop, recording, pause) using a single colored icon. The transport state is received not from the plugin, but from REAPERs default OSC implementation.
In addition, a timer is displayed. The timer displays the remaining time inside the current region. The current region comes from a OSC message "/hulp/region" with two float values start (index 0) + stop (index 1). This will be implemented by me later on, for now only provide the receiving part. This time counts down using REAPERs default position OSC message. Update once every second or so. Not too frequent, but still rather fluent for the user to see.

## Track View

This is a view that shows a number of tracks (received by OSC message "/hulp/track/@). I will provide this implementation inside the plugin myself. For now implement only the reading part. the OSC message will provide
- name (string, index 0): The name
- logical_index (int, index 1): The index. This is not the actual REAPER index, but a logical region-based index used by hulp
- reaper_index: (int, index 2): This is the actual index of the track in REAPER

Show all tracks arranged horizontally. There will never be more than 8 tracks displayed. Always reserve space for 8 tracks, even if there are less. Non-existant tracks are greyed out. I want to see only the "name" and the "logical_index" of each track. In addition, using REAPERs standard OSC messages, highlight record-armed (reddish) and selected tracks (more opacity). You can find them via the "reaper_index" property.

## Event View

This is a queue that shows upcoming events. They are received by OSC "/hulp/upcoming" that has two properties:
- text (string, index 0): shows a description of the event
- time (fooat, index 1): the time in seconds when that event will occur.

Always show the next upcoming event at the top. The other events below. Once an event has happened, remove it from the queue and show the next one. Preview a lookahead list of 5 events maximum.

# Implementation details

- Always use Hsp.OSC for receiving OSC messages.
- All messages that are sent from "hulp" (/hulp) may not be implemented at the moment. Do not bother with modifying "Plugin" in any case, just modify the receiving part. I will provide the sender later on.
- Organize the components into blazor components
- Use as little external dependencies as possible (excpet the ones already present)