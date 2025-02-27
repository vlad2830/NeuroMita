import base64
import json
import os


class SettingsManager:
    def __init__(self, config_path):
        self.config_path = config_path
        self.settings = {}
        self.load_settings()

    def load_settings(self):
        try:
            if os.path.exists(self.config_path):
                with open(self.config_path, "rb") as f:
                    encoded = f.read()
                decoded = base64.b64decode(encoded)
                self.settings = json.loads(decoded.decode("utf-8"))
        except Exception as e:
            print(f"Error loading settings: {e}")
            self.settings = {}

    def save_settings(self):
        try:
            json_data = json.dumps(self.settings, ensure_ascii=False)
            encoded = base64.b64encode(json_data.encode("utf-8"))
            with open(self.config_path, "wb") as f:
                f.write(encoded)
        except Exception as e:
            print(f"Error saving settings: {e}")

    def get(self, key, default=None):
        return self.settings.get(key, default)

    def set(self, key, value):
        self.settings[key] = value