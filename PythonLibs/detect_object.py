import re
import sys
import cv2
import numpy as np
import tensorflow as tf
from collections import defaultdict

def load_model(model_path):
    return tf.saved_model.load(model_path)

def run_inference(model, image):
    input_tensor = tf.convert_to_tensor(image)
    input_tensor = input_tensor[tf.newaxis, ...]

    output_dict = model(input_tensor)
    num_detections = int(output_dict.pop('num_detections'))

    for key in output_dict:
        output_dict[key] = output_dict[key][0, :num_detections].numpy()

    return output_dict

def parse_label_map(label_map_path):
    with open(label_map_path, 'r') as f:
        label_map_string = f.read()

    items = re.findall('item {[^}]+}', label_map_string)
    label_map_dict = {}

    for item in items:
        match = re.search('id\s*:\s*(\d+)', item)
        if not match:
            raise ValueError(f"Invalid label_map format: 'id' not found in {item}")

        class_id = int(match.group(1))

        match = re.search('(?:name|display_name)\s*:\s*\'([^"]+)\'', item)
        if not match:
            raise ValueError(f"Invalid label_map format: 'display_name' or 'name' not found in {item}")

        class_name = match.group(1)
        label_map_dict[class_id] = class_name

    return label_map_dict

def iou(box1, box2):
    ymin1, xmin1, ymax1, xmax1 = box1
    ymin2, xmin2, ymax2, xmax2 = box2

    xmin_intersection = max(xmin1, xmin2)
    ymin_intersection = max(ymin1, ymin2)
    xmax_intersection = min(xmax1, xmax2)
    ymax_intersection = min(ymax1, ymax2)

    intersection_area = max(0, xmax_intersection - xmin_intersection) * max(0, ymax_intersection - ymin_intersection)

    area_box1 = (xmax1 - xmin1) * (ymax1 - ymin1)
    area_box2 = (xmax2 - xmin2) * (ymax2 - ymin2)
    union_area = area_box1 + area_box2 - intersection_area

    return intersection_area / union_area

def detect_objects(image_path, model_path, label_map_path):
    model = load_model(model_path)
    image = cv2.imread(image_path)
    image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    image_height, image_width, _ = image_rgb.shape

    output_dict = run_inference(model, image_rgb)
    detected_classes = output_dict['detection_classes']
    detected_scores = output_dict['detection_scores']
    detected_boxes = output_dict['detection_boxes']

    label_map = parse_label_map(label_map_path)
    confidence_threshold = 0.3
    iou_threshold = 0.1

    filtered_detected_class_names = []
    filtered_detected_boxes = []
    filtered_coordinates = []

    for class_id, score, box in zip(detected_classes, detected_scores, detected_boxes):
        if score >= confidence_threshold:
            is_overlapping = False
            x = (xmax+xmin)/2
            y = (ymax+ymin)/2
            ymin, xmin, ymax, xmax = box
            box = [ymin * image_height, xmin * image_width, ymax * image_height, xmax * image_width]
            for filtered_box, filtered_class_name in zip(filtered_detected_boxes, filtered_detected_class_names):
                if label_map[class_id] == filtered_class_name and iou(box, filtered_box) > iou_threshold:
                    is_overlapping = True
                    break
            
            if not is_overlapping:
                filtered_detected_class_names.append(label_map[class_id])
                filtered_detected_boxes.append(box)
                filtered_coordinates.append([x,y])

    return filtered_detected_class_names, filtered_coordinates

if __name__ == "__main__":
    image_path = sys.argv[1]
    model_path = "./TensorFlow/workspace/training_demo/exported-models/my_model/saved_model"
    label_map_path = "./TensorFlow/workspace/training_demo/annotations/label_map.pbtxt"
    detected_class_names, detected_class_coordinates = detect_objects(image_path, model_path, label_map_path)
    print(",".join(detected_class_names))
    print(",".join(detected_class_coordinates))
