"""Package previously rendered, normalized diagnostic frames as an offline HTML."""
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[1]
data=Path(sys.argv[1]).read_text()
(root/'Animation-Preview.html').write_text((root/'tools/preview-template.html').read_text().replace('/*FRAME_DATA*/',data),encoding='utf-8')
