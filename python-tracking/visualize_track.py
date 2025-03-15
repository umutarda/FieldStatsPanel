import json
import sys

import cv2
import matplotlib.patches as mpatches
import matplotlib.pyplot as plt
import numpy as np
from matplotlib.widgets import Button, Slider


def visualize_tracking_data(json_path, video0_path, video1_path):
    """
    Visualize formatted tracking data from a JSON file and overlay it on a composite video
    created by placing two input videos side by side.

    The JSON is assumed to be a list of frames, where each frame is a dictionary with:
      - "fr": frame number (for display)
      - "obj": a list of object dictionaries. Each object should have:
            "id": track id,
            "class_id": class id,
            "c": [x, y] center coordinate (already reverse-transformed and rounded to 1 decimal),
            "src": source indicator (0 or 1)

    For visualization, if an object’s "src" is 0, its center is drawn as-is; if "src" is 1,
    1920 is added to its x coordinate before plotting. Video frames are read from both input videos,
    starting at frame index + 7372 for debugging purposes, and then concatenated horizontally.

    Parameters:
        json_path (str): Path to the JSON file with formatted tracking data.
        video0_path (str): Path to the left video file.
        video1_path (str): Path to the right video file.
    """
    # Load the tracking data (formatted JSON)
    with open(json_path) as json_file:
        tracking_data = json.load(json_file)
        tracking_data = tracking_data["tracks"]

    # Define known classes and assign distinct colors
    known_classes = [0, 1, 2, 3]  # Adjust if needed
    class_colors = {
        cls: plt.cm.tab10(i / len(known_classes)) for i, cls in enumerate(known_classes)
    }

    # Initialize video captures for both videos
    cap0 = cv2.VideoCapture(video0_path)
    cap1 = cv2.VideoCapture(video1_path)
    total_frames0 = int(cap0.get(cv2.CAP_PROP_FRAME_COUNT))
    total_frames1 = int(cap1.get(cv2.CAP_PROP_FRAME_COUNT))
    min(total_frames0, total_frames1)

    # Create figure and axis for plotting
    fig, ax = plt.subplots(figsize=(12, 6))
    ax.set_facecolor("black")

    current_frame = {"index": 0}

    def update_plot():
        """Update the plot for the current frame."""
        ax.clear()
        ax.set_facecolor("black")
        frame_idx = current_frame["index"]

        frame_info = tracking_data[frame_idx]

        # Set both videos to the corresponding frame (offset +7372)
        cap0.set(cv2.CAP_PROP_POS_FRAMES, frame_idx + 7372)
        cap1.set(cv2.CAP_PROP_POS_FRAMES, frame_idx + 7372)
        ret0, frame0 = cap0.read()
        ret1, frame1 = cap1.read()
        if not ret0 or not ret1:
            print("Failed to read one of the video frames.")
            return

        # Combine the two frames side by side
        combined_frame = np.hstack([frame0, frame1])
        combined_height, combined_width = combined_frame.shape[:2]

        # Display object count text
        ax.text(
            5,
            10,
            f"object count: {len(frame_info['obj'])}",
            color="white",
            fontsize=8,
            bbox=dict(facecolor="black", alpha=0.7, edgecolor="none", pad=1),
        )

        # Plot each object's center; adjust x if src == 1
        for obj in frame_info["obj"]:
            class_id = obj["class_id"]
            center = obj["c"]
            track_id = obj["id"]
            src = obj["src"]

            # If src is 1, add 1920 to x-coordinate.
            display_x = center[0] if src == 0 else center[0] + 1920
            display_y = center[1]

            color = class_colors.get(class_id, "white")
            ax.scatter(
                display_x,
                display_y,
                color=color,
                edgecolors="white",
                linewidth=1.5,
                s=50,
            )
            ax.text(
                display_x - 7,
                display_y - 5,
                f"ID: {track_id}",
                color="white",
                fontsize=8,
                bbox=dict(facecolor="black", alpha=0.7, edgecolor="none", pad=1),
            )

        # Overlay the composite video frame as background
        frame_rgb = cv2.cvtColor(combined_frame, cv2.COLOR_BGR2RGB)
        ax.imshow(frame_rgb, alpha=0.9, extent=[0, combined_width, combined_height, 0])

        # Create legend for classes
        handles = [
            mpatches.Patch(color=color, label=f"Class {cls}")
            for cls, color in class_colors.items()
            if any(obj["class_id"] == cls for obj in frame_info["obj"])
        ]
        ax.legend(
            handles=handles, loc="upper right", facecolor="white", edgecolor="black"
        )

        ax.set_title(f"Frame {frame_info['fr']}/{len(tracking_data)}", color="white")
        ax.set_xlabel("X", color="white")
        ax.set_ylabel("Y", color="white")
        ax.tick_params(axis="both", colors="white")
        plt.draw()

    def next_frame(event=None):
        """Navigate to the next frame."""
        current_frame["index"] = (current_frame["index"] + 1) % len(tracking_data)
        update_plot()
        slider.set_val(current_frame["index"])

    def prev_frame(event=None):
        """Navigate to the previous frame."""
        current_frame["index"] = (current_frame["index"] - 1) % len(tracking_data)
        update_plot()
        slider.set_val(current_frame["index"])

    def on_slider_change(val):
        """Handle slider change events."""
        current_frame["index"] = int(val)
        update_plot()

    # Create navigation buttons and slider
    ax_prev = plt.axes([0.7, 0.01, 0.1, 0.05])
    ax_next = plt.axes([0.81, 0.01, 0.1, 0.05])
    btn_prev = Button(ax_prev, "Back")
    btn_next = Button(ax_next, "Next")
    btn_prev.on_clicked(prev_frame)
    btn_next.on_clicked(next_frame)

    ax_slider = plt.axes([0.1, 0.01, 0.5, 0.03])
    slider = Slider(ax_slider, "Frame", 0, len(tracking_data) - 1, valinit=0, valstep=1)
    slider.on_changed(on_slider_change)

    # Display the first frame
    update_plot()
    plt.show()

    cap0.release()
    cap1.release()


if __name__ == "__main__":
    if len(sys.argv) != 4:
        print(f"Usage: python {sys.argv[0]} <jsonname> <video0> <video1>")
        sys.exit(1)

    jsonname = sys.argv[1]
    video0 = sys.argv[2]
    video1 = sys.argv[3]

    visualize_tracking_data(jsonname, video0, video1)
