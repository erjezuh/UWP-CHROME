import pathlib
import unittest
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]

class ProjectTests(unittest.TestCase):
    def test_required_files(self):
        for item in ["src/XboxGamingBrowser/XboxGamingBrowser.csproj", "src/XboxGamingBrowser/MainPage.xaml.cs", "src/XboxGamingBrowser/Web/home.html", "src/XboxGamingBrowser/Web/diagnostics.html", "docs/INVESTIGACION.md"]:
            self.assertTrue((ROOT / item).is_file(), item)
    def test_manifest_xml(self):
        ET.parse(ROOT / "src/XboxGamingBrowser/Package.appxmanifest")
    def test_diagnostics_claims(self):
        text = (ROOT / "src/XboxGamingBrowser/Web/diagnostics.html").read_text(encoding="utf-8")
        for token in ["WebAssembly", "webgl2", "SharedArrayBuffer", "Atomics", "getGamepads", "crossOriginIsolated"]:
            self.assertIn(token, text)
        self.assertIn("createObjectURL", text)
    def test_appx_logos_have_manifest_dimensions(self):
        import struct
        expected = {"StoreLogo.png": (50, 50), "Logo.png": (150, 150), "SmallLogo.png": (44, 44), "WideLogo.png": (310, 150)}
        for name, dimensions in expected.items():
            data = (ROOT / "src/XboxGamingBrowser/Assets" / name).read_bytes()
            self.assertEqual(data[:8].hex(), "89504e470d0a1a0a", name)
            self.assertEqual(struct.unpack(">II", data[16:24]), dimensions, name)

    def test_no_game_binaries_tracked(self):
        for path in ROOT.rglob("*"):
            self.assertFalse(path.suffix.lower() in {".wasm", ".pak", ".ff", ".zone"}, str(path))

if __name__ == "__main__":
    unittest.main()
