using System;
using Godot;

public static class SaveManager {
	public const string SaveFilePath = "user://savegame.json";
	public const int CurrentSaveVersion = 1;

	public static readonly string[] ValidLevelScenes = new[] {
		"res://Scenes/Levels/Level_1.tscn",
		"res://Scenes/Levels/Level_2.tscn",
		"res://Scenes/Levels/Level_3.tscn",
		"res://Scenes/Levels/Level_4.tscn",
		"res://Scenes/Levels/Level_5.tscn"
	};

	public static bool HasSaveFile() {
		return FileAccess.FileExists(SaveFilePath);
	}

	public static bool HasValidSave() {
		return LoadSave() != null;
	}

	public static bool Save(string levelScenePath, int crystalCount) {
		try {
			if (string.IsNullOrWhiteSpace(levelScenePath)) {
				GD.PushWarning("[SaveManager] Save failed: levelScenePath is null or empty.");
				return false;
			}

			// Validate scene exists in resource system
			if (!ResourceLoader.Exists(levelScenePath)) {
				GD.PushWarning($"[SaveManager] Save failed: Scene does not exist '{levelScenePath}'.");
				return false;
			}

			int levelIndex = GetLevelIndexFromPath(levelScenePath);
			int safeCrystals = Mathf.Max(0, crystalCount);

			var saveDict = new Godot.Collections.Dictionary {
				{ "version", CurrentSaveVersion },
				{ "timestamp", DateTime.UtcNow.ToString("o") },
				{ "current_level", levelScenePath },
				{ "level_index", levelIndex },
				{ "crystal_count", safeCrystals }
			};

			string jsonString = Json.Stringify(saveDict, "\t");

			using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Write);
			if (file == null) {
				Error err = FileAccess.GetOpenError();
				GD.PushError($"[SaveManager] Failed to open '{SaveFilePath}' for writing: {err}");
				return false;
			}

			file.StoreString(jsonString);
			file.Flush();

			GD.Print($"[SaveManager] Progress successfully saved: Level={levelScenePath} (index {levelIndex}), Crystals={safeCrystals}");
			return true;
		} catch (Exception ex) {
			GD.PushError($"[SaveManager] Exception during Save: {ex.Message}");
			return false;
		}
	}

	public static SaveData LoadSave() {
		try {
			if (!FileAccess.FileExists(SaveFilePath)) {
				return null;
			}

			using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Read);
			if (file == null) {
				Error err = FileAccess.GetOpenError();
				GD.PushWarning($"[SaveManager] Could not open save file for reading: {err}");
				return null;
			}

			string content = file.GetAsText();
			if (string.IsNullOrWhiteSpace(content)) {
				GD.PushWarning("[SaveManager] Save file is empty.");
				return null;
			}

			var parsed = Json.ParseString(content);
			if (parsed.VariantType != Variant.Type.Dictionary) {
				GD.PushWarning("[SaveManager] Corrupted save: Root JSON element is not an object.");
				return null;
			}

			var dict = parsed.AsGodotDictionary();

			// 1. Version validation
			if (!dict.ContainsKey("version")) {
				GD.PushWarning("[SaveManager] Corrupted save: Missing 'version' field.");
				return null;
			}
			int version = dict["version"].AsInt32();
			if (version != CurrentSaveVersion) {
				GD.PushWarning($"[SaveManager] Unsupported save version: {version}. Expected: {CurrentSaveVersion}.");
				return null;
			}

			// 2. Current level validation
			if (!dict.ContainsKey("current_level")) {
				GD.PushWarning("[SaveManager] Corrupted save: Missing 'current_level' field.");
				return null;
			}
			string currentLevel = dict["current_level"].AsString();
			if (string.IsNullOrWhiteSpace(currentLevel) || !ResourceLoader.Exists(currentLevel)) {
				GD.PushWarning($"[SaveManager] Corrupted save: 'current_level' scene does not exist '{currentLevel}'.");
				return null;
			}

			// 3. Crystal count validation
			int crystalCount = 0;
			if (dict.ContainsKey("crystal_count")) {
				crystalCount = Mathf.Max(0, dict["crystal_count"].AsInt32());
			}

			int levelIndex = dict.ContainsKey("level_index")
				? dict["level_index"].AsInt32()
				: GetLevelIndexFromPath(currentLevel);

			string timestamp = dict.ContainsKey("timestamp")
				? dict["timestamp"].AsString()
				: "";

			return new SaveData {
				Version = version,
				Timestamp = timestamp,
				CurrentLevel = currentLevel,
				LevelIndex = levelIndex,
				CrystalCount = crystalCount
			};
		} catch (Exception ex) {
			GD.PushWarning($"[SaveManager] Exception parsing save data: {ex.Message}");
			return null;
		}
	}

	public static bool DeleteSave() {
		try {
			if (!FileAccess.FileExists(SaveFilePath)) {
				return true;
			}

			Error err = DirAccess.RemoveAbsolute(SaveFilePath);
			if (err == Error.Ok) {
				GD.Print($"[SaveManager] Save file deleted at '{SaveFilePath}'.");
				return true;
			} else {
				GD.PushWarning($"[SaveManager] Failed to delete save file '{SaveFilePath}': {err}");
				return false;
			}
		} catch (Exception ex) {
			GD.PushError($"[SaveManager] Exception deleting save: {ex.Message}");
			return false;
		}
	}

	public static int GetLevelIndexFromPath(string path) {
		if (string.IsNullOrEmpty(path)) return 1;
		if (path.Contains("Level_1")) return 1;
		if (path.Contains("Level_2")) return 2;
		if (path.Contains("Level_3")) return 3;
		if (path.Contains("Level_4")) return 4;
		if (path.Contains("Level_5")) return 5;
		return 1;
	}
}

public class SaveData {
	public int Version { get; set; } = 1;
	public string Timestamp { get; set; } = "";
	public string CurrentLevel { get; set; } = "res://Scenes/Levels/Level_1.tscn";
	public int LevelIndex { get; set; } = 1;
	public int CrystalCount { get; set; } = 0;
}
