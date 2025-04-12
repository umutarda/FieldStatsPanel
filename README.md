# YOLO Tracking Panel Project Overview

This project is a sophisticated object tracking system that combines Unity for the front-end user interface with Python-based tracking algorithms on the backend. Let me break down how it works:

## Core Components

### 1. Unity Front-End

- **Video Display**: Shows video with detection overlays
- **Interactive UI**: Allows users to select objects to track
- **Visual Feedback**: Displays bounding boxes from YOLO and tracking markers

### 2. Python Back-End

- **ByteTrack Algorithm**: Advanced object tracking system
- **Coordinate Transformation**: Maps between different coordinate spaces
- **RPC Server**: Communicates with Unity frontend

## How It Works (Data Flow)

1. **Initial Detection**:

   - The system loads YOLO detection data from a JSON file
   - Detections are displayed as red boxes in the UI

2. **User Interaction**:

   - Users can click on objects in the video to select them for tracking
   - The "Bottom Panel" shows available ID numbers for new tracks
   - When an object is selected, an ID is assigned to it

3. **Tracking Process**:

   - Selected objects are sent to the Python backend via RPC
   - The `tracker.py` uses ByteTrack to track these objects across frames
   - Tracking data comes back and is displayed as small circles with ID numbers

4. **Visualization**:
   - Each tracked object shows its ID
   - Users can toggle displays with keyboard shortcuts:
     - `A`: Show all tracking overlays
     - `N`: Hide all tracking overlays
     - `L`: Toggle display of lost tracks
     - `O`: Toggle between current and old tracking data

## Key Technical Details

### Coordinate Systems

The system handles two camera views (left and right) with different coordinate spaces:

- Each source has its own homography matrix for coordinate transformations
- `transform_utility.py` handles conversion between coordinate spaces

### Object Tracking

- Uses ByteTrack algorithm for reliable tracking across frames
- Handles temporary occlusions with interpolation
- Tracks can be "lost" if objects disappear for too long (> 10 frames)

### Communication

The Unity and Python parts communicate through a JSON-RPC system:

1. Unity prepares tracking requests with `TrackingManagerRpc.cs`
2. Python processes these requests through `rpc.py` and `app.py`
3. Results are sent back to Unity and displayed

### State Management

- The `TrackingManager.cs` maintains tracking state
- `OverlayPropertiesManager.cs` stores properties for each tracked object
- `SingletonManager.cs` provides centralized access to key components

The architecture is designed to be flexible and modular, with clear separation between the tracking logic (Python) and the visualization/interaction layer (Unity). This allows for different tracking algorithms to be swapped in without changing the UI.
