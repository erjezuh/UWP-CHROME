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

    def test_experimental_edge_namespace_identity_matches_dev_signing_subject(self):
        ns = {"f": "http://schemas.microsoft.com/appx/manifest/foundation/windows10"}
        root = ET.parse(ROOT / "src/XboxGamingBrowser/Package.appxmanifest").getroot()
        identity = root.find("f:Identity", ns)
        self.assertEqual(identity.get("Name"), "Microsoft.MicrosoftEdge.XboxGamingBrowser")
        self.assertEqual(identity.get("Publisher"), "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US")
        app = root.find(".//f:Application", ns)
        self.assertEqual(app.get("Id"), "App")
        package_script = (ROOT / "package.ps1").read_text(encoding="utf-8")
        self.assertIn("New-SelfSignedCertificate", package_script)
        self.assertIn("-Subject $publisher", package_script)
        self.assertIn("not a Microsoft-issued certificate", package_script)
    def test_diagnostics_claims(self):
        text = (ROOT / "src/XboxGamingBrowser/Web/diagnostics.html").read_text(encoding="utf-8")
        for token in ["WebAssembly", "webgl2", "webgl'", "SharedArrayBuffer", "Atomics", "getGamepads", "crossOriginIsolated", "Ratón y puntero"]:
            self.assertIn(token, text)
        self.assertIn("createObjectURL", text)
    def test_google_search_and_mouse_input_are_wired(self):
        code = (ROOT / "src/XboxGamingBrowser/MainPage.xaml.cs").read_text(encoding="utf-8")
        xaml = (ROOT / "src/XboxGamingBrowser/MainPage.xaml").read_text(encoding="utf-8")
        self.assertIn("www.google.es/search?q=", code)
        self.assertIn('PointerPressed="Browser_PointerPressed"', xaml)
        self.assertIn("Browser.Focus(FocusState.Pointer)", code)

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
