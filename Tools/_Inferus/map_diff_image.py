from io import BytesIO
import math

from PIL import Image

MAX_IMAGE_BYTES = 20 * 1024 * 1024


def save_png(image, path, max_bytes=MAX_IMAGE_BYTES):
    """Keep native pixels when possible; resize only after lossless compression fails."""
    if max_bytes <= 0:
        raise ValueError("max_bytes must be positive")

    output = BytesIO()
    image.save(output, format="PNG")
    if output.tell() <= max_bytes:
        with open(path, "wb") as target:
            target.write(output.getvalue())
        return

    original_size = image.size
    candidate = image
    while True:
        output = BytesIO()
        candidate.save(output, format="PNG", optimize=True, compress_level=9)
        size = output.tell()
        if size <= max_bytes:
            with open(path, "wb") as target:
                target.write(output.getvalue())
            if candidate.size != original_size:
                print(f"Resized oversized PNG {path}: {original_size} -> {candidate.size}, {size} bytes")
            return

        if candidate.size == (1, 1):
            raise ValueError(f"PNG cannot fit within {max_bytes} bytes: {path}")

        # Leave headroom because encoded size does not scale exactly with pixel count.
        scale = min(0.95, math.sqrt(max_bytes / size) * 0.95)
        width = max(1, int(candidate.width * scale))
        height = max(1, int(candidate.height * scale))
        candidate = image.resize((width, height), Image.Resampling.LANCZOS)
