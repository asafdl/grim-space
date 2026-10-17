#!/usr/bin/env python3
"""
Analyze video with Qwen2-VL using Hugging Face Transformers.
Improved version of analyze_video.py with per-frame timing, better error handling,
and cleaner output for me-vs-Qwen comparison.

Usage:
    python analyze_video2.py <video_path> "<prompt>" [--model Qwen/Qwen2-VL-2B-Instruct] [--max-frames 8] [--resize 384]

Example:
    python analyze_video2.py "teaser_fixed2.avi" "Yes/No questions..." --max-frames 4 --resize 384
"""

import argparse
import cv2
import os
import sys
import tempfile
import time
import torch
from transformers import Qwen2VLForConditionalGeneration, AutoProcessor
from qwen_vl_utils import process_vision_info


def extract_frames_as_files(video_path, max_frames=8, resize_to=384):
    """Extract evenly spaced frames from video, resize, save as temp files."""
    cap = cv2.VideoCapture(video_path)
    total_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
    fps = cap.get(cv2.CAP_PROP_FPS)
    
    if total_frames == 0:
        raise ValueError("Could not read video")
    
    frame_indices = [int(i * total_frames / max_frames) for i in range(max_frames)]
    temp_dir = tempfile.mkdtemp(prefix="qwen_video2_")
    frame_paths = []
    
    for i, idx in enumerate(frame_indices):
        cap.set(cv2.CAP_PROP_POS_FRAMES, idx)
        ret, frame = cap.read()
        if ret:
            frame = cv2.resize(frame, (resize_to, resize_to), interpolation=cv2.INTER_AREA)
            frame_path = os.path.join(temp_dir, f"frame_{i:04d}.jpg")
            cv2.imwrite(frame_path, frame, [cv2.IMWRITE_JPEG_QUALITY, 85])
            frame_paths.append((i, idx, frame_path))
    
    cap.release()
    return frame_paths, temp_dir, fps, total_frames


def analyze_single_frame(model, processor, frame_path, frame_num, total_frames, prompt):
    """Analyze a single frame with Qwen2-VL. Returns (response, elapsed_seconds)."""
    messages = [{
        "role": "user",
        "content": [
            {"type": "image", "image": frame_path},
            {"type": "text", "text": f"[Frame {frame_num}/{total_frames}] {prompt}"}
        ]
    }]
    
    text = processor.apply_chat_template(messages, tokenize=False, add_generation_prompt=True)
    image_inputs, video_inputs = process_vision_info(messages)
    
    inputs = processor(
        text=[text],
        images=image_inputs,
        videos=video_inputs,
        padding=True,
        return_tensors="pt"
    ).to(model.device)
    
    start = time.perf_counter()
    with torch.no_grad():
        generated_ids = model.generate(**inputs, max_new_tokens=256)
    elapsed = time.perf_counter() - start
    
    generated_ids_trimmed = [
        out_ids[len(in_ids):] for in_ids, out_ids in zip(inputs.input_ids, generated_ids)
    ]
    response = processor.batch_decode(
        generated_ids_trimmed, skip_special_tokens=True, clean_up_tokenization_spaces=False
    )[0]
    
    return response.strip(), elapsed


def main():
    parser = argparse.ArgumentParser(description="Analyze video with Qwen2-VL (per-frame, low-res, with timing)")
    parser.add_argument("video_path", help="Path to video file")
    parser.add_argument("prompt", help="Analysis prompt for each frame")
    parser.add_argument("--model", default="Qwen/Qwen2-VL-2B-Instruct", help="Model name/path")
    parser.add_argument("--max-frames", type=int, default=4, help="Max frames to analyze")
    parser.add_argument("--resize", type=int, default=384, help="Resize frames to NxN pixels")
    args = parser.parse_args()

    print(f"Loading model: {args.model}")
    load_start = time.perf_counter()
    model = Qwen2VLForConditionalGeneration.from_pretrained(
        args.model,
        torch_dtype=torch.bfloat16,
        device_map="auto",
        trust_remote_code=True
    )
    processor = AutoProcessor.from_pretrained(args.model, trust_remote_code=True)
    load_elapsed = time.perf_counter() - load_start
    print(f"Model loaded in {load_elapsed:.2f} seconds")

    print(f"Extracting frames from: {args.video_path}")
    frame_paths, temp_dir, fps, total_frames = extract_frames_as_files(
        args.video_path, args.max_frames, args.resize
    )
    print(f"Extracted {len(frame_paths)} frames at {args.resize}x{args.resize} (FPS={fps:.1f}, total_frames={total_frames})")

    try:
        all_results = []
        for i, (frame_idx, video_frame_idx, frame_path) in enumerate(frame_paths):
            print(f"\n--- Analyzing frame {i+1}/{len(frame_paths)} (video frame {video_frame_idx}) ---")
            result, elapsed = analyze_single_frame(
                model, processor, frame_path, i+1, len(frame_paths), args.prompt
            )
            all_results.append((i+1, video_frame_idx, result, elapsed))
            print(f"Frame {i+1}: {result[:300]}...  ({elapsed:.2f}s)")

        print("\n" + "="*60)
        print("COMPLETE FRAME-BY-FRAME ANALYSIS:")
        print("="*60)
        total_inference = 0
        for frame_num, video_frame_idx, result, elapsed in all_results:
            print(f"\n--- Frame {frame_num} (video frame {video_frame_idx}, {elapsed:.2f}s) ---")
            print(result)
            total_inference += elapsed

        print("\n" + "="*60)
        print("SUMMARY:")
        print("="*60)
        print(f"Model load time:  {load_elapsed:.2f}s")
        print(f"Total inference:  {total_inference:.2f}s across {len(all_results)} frames")
        print(f"Per-frame avg:    {total_inference/len(all_results):.2f}s")
        print(f"Grand total:      {load_elapsed + total_inference:.2f}s")
    finally:
        for _, _, fp in frame_paths:
            try:
                os.remove(fp)
            except:
                pass
        try:
            os.rmdir(temp_dir)
        except:
            pass


if __name__ == "__main__":
    main()
