import json

import tracker  # Assuming tracker module is available
from app import update_data


def main():
    # Create test input with frame_id 7200 and an empty coord_id.
    test_payload = {
        "frame_id": 7372,
        "coord_id": {},  # This represents an empty coord_id
    }

    # Call update_data with the test payload.
    result = update_data(test_payload)

    # Write result to a JSON file.
    with open("result.json", "w") as json_file:
        json.dump(result, json_file, indent=4)


if __name__ == "__main__":
    main()
