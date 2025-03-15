import numpy as np


def reverse_transform_point(point):
    """
    Reverse transforms a 2D point using one of two homography matrices.

    For points where the x-coordinate is greater than 347, it subtracts 347 and reads
    the homography matrix from "al1_homography_matrix.txt". Otherwise, it uses the matrix from
    "al2_homography_matrix.txt". Then, it computes the inverse and applies it.

    Parameters:
        point (list or tuple): The [x, y] coordinate to reverse transform.

    Returns:
        isRight (bool): True if the original x was > 347, else False.
        new_point (list): The reverse-transformed [x, y] coordinate.
    """
    x, y = point
    isRight = x > 347
    # Select the appropriate homography matrix file based on the x-coordinate.
    H = np.loadtxt(
        "al1_homography_matrix.txt" if isRight else "al2_homography_matrix.txt"
    )
    # If the point is from the right side, adjust x by subtracting 347.
    x_adjusted = x - 347 if isRight else x
    # Compute the inverse of the homography matrix.
    H_inv = np.linalg.inv(H)
    # Convert the adjusted point to homogeneous coordinates.
    homogeneous_point = np.array([x_adjusted, y, 1])
    # Apply the inverse transformation.
    orig_point = H_inv.dot(homogeneous_point)
    # Normalize to convert back to Cartesian coordinates.
    orig_point /= orig_point[2]
    return isRight, [float(orig_point[0]), float(orig_point[1])]


import numpy as np


def transform_point(point, src):
    """
    Forward transforms a 2D point using one of two homography matrices.

    For src=0, the function uses the homography matrix from "al2_homography_matrix.txt".
    For src=1, it uses the homography matrix from "al1_homography_matrix.txt".
    After applying the forward transformation, if src==1, 347 is added to the x-coordinate
    of the resulting point.

    Parameters:
        point (list or tuple): The [x, y] coordinate to transform.
        src (int): Source indicator. 0 means use "al2_homography_matrix.txt"; 1 means use "al1_homography_matrix.txt".

    Returns:
        new_point (list): The forward-transformed [x, y] coordinate. If src==1, the x value is increased by 347.
    """
    # Load the appropriate homography matrix based on src
    H = (
        np.loadtxt("al2_homography_matrix.txt")
        if src == 0
        else np.loadtxt("al1_homography_matrix.txt")
    )

    # Convert the input point to homogeneous coordinates
    homogeneous_point = np.array([point[0], point[1], 1])

    # Apply the forward transformation
    transformed = H.dot(homogeneous_point)
    transformed /= transformed[2]  # Normalize

    new_point = [float(transformed[0]), float(transformed[1])]

    # If src is 1, add 347 to the x-coordinate.
    if src == 1:
        new_point[0] += 347

    return new_point


# Example usage:
# if __name__ == "__main__":
#     with open("test.json", "r") as f:
#         tracking_data = json.load(f)

#     updated_tracking_data = format_tracking_data(tracking_data)

#     with open("tracking_data_reversed.json", "w") as f:
#         json.dump(updated_tracking_data, f, indent=4)

#     print("Tracking data formatted and updated with reverse-transformed centers and src property.")
