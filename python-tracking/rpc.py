import argparse
import time
from peaceful_pie.unity_comms import UnityComms
from app import update_data 

def run(args: argparse.Namespace) -> None:
    unity_comms = UnityComms(port=args.port)
    
    while True:
        # Wait until Unity reports that it is ready.
        while not unity_comms.IsReady():
            print("Waiting for Unity to be ready...")
            time.sleep(0.5)
        
        print("Unity is ready. Requesting track data...")
        # Get the TrackRequest from Unity.
        track_request = unity_comms.GetRequest()
        print("TrackRequest received:")
        print(track_request)
        
        # Process the track request using the update function.
        # The update function is expected to return a dict that matches the UpdateResult struct.
        update_result = update_data(track_request)
        print("UpdateResult from processing:")
        print(update_result)
        
        # Send the update result back to Unity.
        unity_comms.OnReceive(updateResult=update_result)
        print("UpdateResult sent back to Unity.")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=9000)
    args = parser.parse_args()
    run(args)
