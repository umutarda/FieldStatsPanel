import json

import numpy as np
import supervision as sv  # Includes ByteTrack implementation

from transform_utility import reverse_transform_point, transform_point

CHUNK_LENGTH = 1800


def update(start_frame, coord_ids):
    """Update the start mapping based on coord_ids, filter the JSON data, and
    then perform tracking with the filtered data.

    :param start_frame: The frame index from which to start processing.
    :param coord_ids: A dictionary mapping 2D coordinate arrays (or
        string representations of them) to an integer id.
    :return: A tuple (frame_index, lost_ids, tracking_result) where
        tracking_result is a JSON-like dict.
    """
    # Load original JSON from disk
    with open("radon.json") as f:
        input_data = json.load(f)

    # Find the start frame data
    start_frame_data = next(
        (frame for frame in input_data if frame.get("frame_index") == start_frame), None
    )
    if start_frame_data is None:
        raise ValueError(f"Start frame {start_frame} not found in radon.json")

    # Create start_map: mapping from object index in the start frame to the assigned id
    start_map = {}  # {object_index: assigned_id}
    objects = start_frame_data.get("objects", [])

    for mapping in coord_ids:
        # mapping is expected to be a dict with keys "id", "c", and "src"
        coord = transform_point(
            mapping["c"], mapping["src"]
        )  # This is a list like [x, y]


        assigned_id = mapping["id"]  # The assigned id

        # Convert the coordinate into a tuple for comparison
        coord_tuple = tuple(coord)
        match_found = False

        print(assigned_id,": ",coord)
        min_distance = float('inf')
        min_index = None
        min_transformed_center = tuple()
        
        for idx, obj in enumerate(objects):
            transformed_center = obj.get("transformed_center")
            if obj.get("source") == "right":
                transformed_center = [transformed_center[0]+347,transformed_center[1]]
                
            if transformed_center is not None:
                if isinstance(transformed_center, list):
                    transformed_center = tuple(transformed_center)
                distance = np.linalg.norm(np.array(transformed_center) - np.array(coord_tuple))
                #print(f"Mapping {assigned_id}: Object {idx} with center {transformed_center} has distance {distance}")
                if distance < min_distance:
                    min_distance = distance
                    min_index = idx
                    min_transformed_center = transformed_center


        # Set the value for the object with the minimum distance
        if min_index is not None:
            start_map[min_index] = assigned_id
            print("Minimum distance match found for", assigned_id, "at index", min_index, "with distance", min_distance," with t_c: ",min_transformed_center)
        #else:
            #print("No valid transformed_center found.")

        # for idx, obj in enumerate(objects):
        #     transformed_center = obj.get("transformed_center")
        #     if transformed_center is not None:
        #         # Ensure transformed_center is a tuple for consistency
        #         if isinstance(transformed_center, list):
        #             transformed_center = tuple(transformed_center)
                
        #         # Convert to NumPy arrays
        #         center_arr = np.array(transformed_center)
        #         coord_arr = np.array(coord_tuple)
                
        #         # Calculate the Euclidean distance between the two points
        #         distance = np.linalg.norm(center_arr - coord_arr)
                
        #         # Check if the distance is within a desired threshold
        #         if distance < 10:  # Replace 1 with your desired threshold
        #             start_map[idx] = assigned_id
        #             match_found = True
        #             print("match found for", assigned_id, "with distance:", distance)
        #             break

        # for idx, obj in enumerate(objects):
        #     transformed_center = obj.get("transformed_center")
        #     if transformed_center is not None:
        #         # Ensure transformed_center is a tuple for comparison
        #         if isinstance(transformed_center, list):
        #             transformed_center = tuple(transformed_center)
        #         # Use np.allclose with a small tolerance to compare coordinates
        #         if np.allclose(transformed_center, coord_tuple, atol=1):
        #             start_map[idx] = assigned_id
        #             match_found = True
        #             print("match found for", assigned_id)
        #             break

        # if not match_found:
        #     print("match not found for", assigned_id)
        #     # No matching object found: add a new object with default values
        #     new_object = {
        #         "transformed_center": list(coord_tuple),
        #         "source": "unknown",
        #         "confidence": 1.0,
        #         "team_index": assigned_id,
        #     }
        #     new_index = len(objects)
        #     objects.append(new_object)
        #     start_map[new_index] = assigned_id

    # Filter the JSON data to include only frames in the desired range
    filtered_data = [
        frame
        for frame in input_data
        if start_frame <= frame.get("frame_index", 0) < start_frame + CHUNK_LENGTH
    ]

    # Feed the filtered JSON data (in-memory) along with the start_map to the tracker
    return perform_tracking_from_json(filtered_data, start_frame, start_map)


def perform_tracking_from_json(input_data, start_frame, start_map):
    """Perform tracking using ByteTrack based on bounding box information from
    input_data.

    :param input_data: List of frame detection dictionaries.
    :param start_frame: The starting frame index.
    :param start_map: Mapping from start frame's object indices to an
        assigned id.
    :return: A tuple (last_frame_index, lost_ids, tracking_result) where
        tracking_result is a JSON-like dict.
    """
    # Initialize ByteTrack
    tracker = sv.ByteTrack(
        track_activation_threshold=0.1,
        minimum_matching_threshold=0.98,
        lost_track_buffer=10,
        frame_rate=59,
        minimum_consecutive_frames=1,
    )

    # Tracking management variables
    max_allowed_id = 23  # Maximum allowed internal id
    active_tracks = {}
    reusable_ids = list(range(1, max_allowed_id + 1))  # Pool of available IDs
    track_id_map = {}  # Map external track ids to internal ids
    frame_count = 0
    active_track_counts = []
    lost_tracker = [0] * 23
    lost_array = set()
    tracking_data = []

    for frame_data in input_data:
        frame_count += 1
        frame_index = frame_data["frame_index"]
        detections = frame_data["objects"]

        bboxes = []
        confidences = []
        class_ids = []
        for obj in detections:
            # Adjust x coordinate for detections coming from "right" if needed
            if obj["source"] == "right":
                obj["transformed_center"][0] += 347
            bbox = [
                obj["transformed_center"][0] - 2.5,
                obj["transformed_center"][1] - 2.5,
                obj["transformed_center"][0] + 2.5,
                obj["transformed_center"][1] + 2.5,
            ]
            bboxes.append(bbox)
            class_ids.append(obj.get("team_index", -1))
            confidences.append(obj["confidence"])

        if bboxes:
            bboxes = np.array(bboxes, dtype=np.float32)
        else:
            bboxes = np.empty((0, 4), dtype=np.float32)

        detection_supervision = sv.Detections(
            xyxy=bboxes,
            confidence=np.array(confidences, dtype=np.float32),
            class_id=np.array(class_ids, dtype=np.int32),
        )

        tracked_objects = tracker.update_with_detections(detection_supervision)
        frame_tracking_data = {"frame_index": frame_index, "objects": []}
        updated_tracks = set()

        for index, track in enumerate(tracked_objects):
            bbox = track[0].tolist()
            confidence = track[2]
            class_id = track[3]
            center_x = (bbox[0] + bbox[2]) / 2
            center_y = (bbox[1] + bbox[3]) / 2

            external_id = track[4]
            if external_id not in track_id_map:
                if reusable_ids:
                    distances = []
                    for internal_id in reusable_ids:
                        if internal_id in active_tracks:
                            if class_id == active_tracks[internal_id]["cls_id"]:
                                prev_center = active_tracks[internal_id]["center"]
                                distance_delta = np.sqrt(
                                    (center_x - prev_center[0]) ** 2
                                    + (center_y - prev_center[1]) ** 2
                                )
                                distances.append((internal_id, distance_delta))
                        else:
                            # For detections in the start frame, try to use forced id from start_map if available
                            if frame_index == start_frame and index in start_map:
                                forced_internal_id = start_map[index]
                                for internal_id in reusable_ids:
                                    if internal_id == forced_internal_id:
                                        distances.append((internal_id, 0))
                                    else:
                                        distances.append((internal_id, float("inf")))
                            else:
                                distances.append((internal_id, 0))
                    if len(distances) == 0:
                        continue
                    min_id, min_distance = min(distances, key=lambda x: x[1])
                    if min_distance > 28:  # Threshold for matching distance
                        continue
                    internal_id = min_id
                    reusable_ids.remove(internal_id)
                    print(
                        "Put,",
                        internal_id,
                        ",at frame",
                        frame_index,
                        f"with distance={min_distance}",
                    )
                    track_id_map[external_id] = internal_id
                    active_tracks[internal_id] = {
                        "frame_count": frame_count,
                        "center": [center_x, center_y],
                        "cls_id": class_id,
                        "active": True,
                    }
                    updated_tracks.add(internal_id)
                    frame_tracking_data["objects"].append(
                        {
                            "track_id": int(internal_id),
                            "class_id": int(active_tracks[internal_id]["cls_id"]),
                            "confidence": float(confidence),
                            "bbox": list(map(float, bbox)),
                            "center": list(map(float, [center_x, center_y])),
                        }
                    )
                else:
                    continue
            else:
                internal_id = track_id_map[external_id]
                active_tracks[internal_id]["frame_count"] = frame_count
                active_tracks[internal_id]["center"] = [center_x, center_y]
                if class_id != active_tracks[internal_id]["cls_id"]:
                    active_tracks[internal_id]["active"] = False
                else:
                    updated_tracks.add(internal_id)
                    frame_tracking_data["objects"].append(
                        {
                            "track_id": int(internal_id),
                            "class_id": int(active_tracks[internal_id]["cls_id"]),
                            "confidence": float(confidence),
                            "bbox": list(map(float, bbox)),
                            "center": list(map(float, [center_x, center_y])),
                        }
                    )

        # Add interpolated detection for active tracks not updated in the current frame
        for internal_id, data in active_tracks.items():
            if internal_id not in updated_tracks:  # data["active"] and
                center = data["center"]
                bbox = [
                    center[0] - 2.5,
                    center[1] - 2.5,
                    center[0] + 2.5,
                    center[1] + 2.5,
                ]
                frame_tracking_data["objects"].append(
                    {
                        "track_id": int(internal_id),
                        "class_id": int(data["cls_id"]),
                        "confidence": 0.0,
                        "bbox": bbox,
                        "center": center,
                    }
                )

        # Manage lost tracks and update reusable ids
        for internal_id, data in list(active_tracks.items()):
            lost = False
            if not data["active"]:
                if internal_id not in reusable_ids:
                    lost = True
            elif frame_count - data["frame_count"] > 10:
                lost = True

            if lost:
                print("Lost", internal_id, "at frame", frame_index)
                active_tracks[internal_id]["active"] = False
                reusable_ids.append(internal_id)
                external_ids_to_remove = [
                    k for k, v in track_id_map.items() if v == internal_id
                ]
                for ext_id in external_ids_to_remove:
                    del track_id_map[ext_id]

        for i in range(23):
            if (i + 1) in active_tracks and not active_tracks[i + 1]["active"]:
                lost_tracker[i] += 1
                if lost_tracker[i] > 120:
                    print("Lost for 1 second, index=", i + 1, "at frame", frame_index)
                    lost_array.add(i + 1)
            else:
                lost_tracker[i] = 0

        if len(lost_array) > 0:
            # Iterate through all tracks in active_tracks
            for internal_id, data in active_tracks.items():
                if not data["active"]:

                    lost_array.add(internal_id)
                    # Use internal_id-1 as index for lost_tracker
                    tracker_index = internal_id - 1
                    # Check if the track has been lost for ≤ 60 frames
                    if tracker_index < len(
                        lost_tracker
                    ):  # and lost_tracker[tracker_index] <= 60:
                        # Add interpolation entry only if not already in the final frame data
                        if not any(
                            d["track_id"] == internal_id
                            for d in frame_tracking_data["objects"]
                        ):
                            center = data["center"]
                            bbox = [
                                center[0] - 2.5,
                                center[1] - 2.5,
                                center[0] + 2.5,
                                center[1] + 2.5,
                            ]
                            frame_tracking_data["objects"].append(
                                {
                                    "track_id": int(internal_id),
                                    "class_id": int(data["cls_id"]),
                                    "confidence": 0.0,  # Interpolated detection
                                    "bbox": bbox,
                                    "center": center,
                                }
                            )
                    # else:
                    #     # For tracks lost > 60 frames, ensure they are added to lost_array (if not already)
                    #     if internal_id not in lost_array:
                    #         lost_array.append(internal_id)

            tracking_data.append(frame_tracking_data)
            sorted_lost_array = sorted(
                lost_array,
                key=lambda track_id: lost_tracker[track_id - 1],
                reverse=True,
            )
            return frame_index, sorted_lost_array, format_tracking_data(tracking_data)

        else:
            tracking_data.append(frame_tracking_data)

        current_active_count = sum(
            1 for track in active_tracks.values() if track["active"]
        )
        active_track_counts.append((frame_index, current_active_count))

    sorted_lost_array = sorted(
        lost_array, key=lambda track_id: lost_tracker[track_id - 1], reverse=True
    )
    return frame_index, sorted_lost_array, format_tracking_data(tracking_data)


def format_tracking_data(tracking_data):
    """
    Formats the tracking data in place by performing the following steps for each frame:
      - Renames "frame_index" to "fr".
      - Renames "objects" to "obj".
      - For each object in each frame:
          - Reverse-transforms the "center" coordinate using reverse_transform_point.
          - Rounds the transformed center coordinates (x, y) to one decimal place.
          - Removes the original "center", "confidence", and "bbox" properties.
          - Renames "track_id" to "id".
          - Sets a new key "c" with the rounded, reverse-transformed center.
          - Sets a new key "src" to 1 if isRight is True (i.e., original x > 347), or 0 otherwise.

    Parameters:
      tracking_data (list): A list of frame tracking dictionaries, where each frame contains
                            a "frame_index" and an "objects" list.

    Returns:
      The updated tracking_data with the new format.
    """
    for frame in tracking_data:
        frame["fr"] = frame["frame_index"]
        del frame["frame_index"]
        frame["obj"] = frame["objects"]
        del frame["objects"]
        if "obj" in frame:
            for obj in frame["obj"]:
                if "center" in obj:
                    isRight, new_center = reverse_transform_point(obj["center"])
                    # Round the transformed center coordinates to 1 decimal point.
                    new_center = [round(coord, 1) for coord in new_center]
                    del obj["center"]
                    if "confidence" in obj:
                        del obj["confidence"]
                    obj["id"] = obj["track_id"]
                    del obj["track_id"]
                    obj["cls_id"] = obj["class_id"]
                    del obj["class_id"]
                    obj["c"] = new_center
                    obj["src"] = 1 if isRight else 0
                    if "bbox" in obj:
                        del obj["bbox"]
    return tracking_data
