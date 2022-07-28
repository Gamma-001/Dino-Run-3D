using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Rendering;

// NOTE : THIS SCRIPT DEPENDS ON THE RELATIVE ORDER OF UI ELEMTS UNDER CANVAS, MAKE SURE ITS ORDER FROM THE TOP IS LEFT UNCHANGED

public class UIController : MonoBehaviour {
	[Header("References")]
	[SerializeField] private GlobalController world;
	[SerializeField] private GameObject btn_pause;

	[Header("Sprites")]
	[SerializeField] private Sprite sprite_paused;
	[SerializeField] private Sprite sprite_playing;

	private bool running = false;

	private float timeScaleBuffer = 1.0f;

	private void Start() {
		// initialize UI
		world.dino.GetComponent<PlayerInput>().enabled = false;
		transform.GetChild(0).gameObject.SetActive(true);	// in game UI
		transform.GetChild(1).gameObject.SetActive(true);	// main menu
		transform.GetChild(2).gameObject.SetActive(false);	// restart menu
		transform.GetChild(3).gameObject.SetActive(false);  // instructions
		transform.GetChild(4).gameObject.SetActive(false);	// settings menu

		transform.GetChild(1).GetChild(1).GetComponent<Button>().interactable = false;  // resume button

		// match settings to stored values
		TMP_Dropdown dropdown = transform.GetChild(4).GetChild(1).GetChild(0).GetChild(2).GetComponent<TMP_Dropdown>();
		Button btn_shadows = transform.GetChild(4).GetChild(1).GetChild(1).GetChild(2).GetComponent<Button>();
		Button btn_postProcessing = transform.GetChild(4).GetChild(1).GetChild(2).GetChild(2).GetComponent<Button>();

		if (PlayerPrefs.HasKey("graphics_quality")) {
			dropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("graphics_quality"));
			SetQuality(dropdown.value);
		}
		if (PlayerPrefs.HasKey("shadows")) {
			int t_val = PlayerPrefs.GetInt("shadows");
			btn_shadows.gameObject.transform.GetChild(0).GetComponent<TMP_Text>().text = t_val == 1 ? "on" : "off";
			if (t_val == 1) world.mainLight.shadows = LightShadows.Soft;
			else world.mainLight.shadows = LightShadows.None;
		}
		if (PlayerPrefs.HasKey("post_processing")) {
			int t_val = PlayerPrefs.GetInt("post_processing");
			btn_postProcessing.gameObject.transform.GetChild(0).GetComponent<TMP_Text>().text = t_val == 1 ? "on" : "off";
			if (t_val == 1) world.postProcessVolume.SetActive(true);
			else world.postProcessVolume.SetActive(false);
		}

		// set the highScore Text
		uint HighScore = 0;
		if (PlayerPrefs.HasKey("high_score")) {
			HighScore = (uint)PlayerPrefs.GetInt("high_score");
		}
		string sHighScore = HighScore.ToString();
		TMP_Text scoreText = transform.GetChild(0).GetChild(2).GetComponent<TMP_Text>();
		scoreText.text = "Best   : " + new string('0', Mathf.Max(7 - sHighScore.Length, 0)) + sHighScore;
	}

	private void Update() {

		// update score text each frame
		TMP_Text scoreText = transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>();
		string sScore = GlobalController.Score.ToString();
		scoreText.text = "Score: " + new string('0', Mathf.Max(7 - sScore.Length, 0)) + sScore;
	}

	// Main Menu
	public void NewGame() {
		if (!running) {
			world.dino.BeginGame();
			world.dino.GetComponent<PlayerInput>().enabled = true;
			transform.GetChild(1).gameObject.SetActive(false);
			running = true;
		}
		else {
			SceneManager.LoadScene(0);
		}
	}

	public void PauseGame() {
		if (btn_pause.GetComponent<Image>().sprite.name == sprite_paused.name) {
			btn_pause.GetComponent<Image>().sprite = sprite_playing;
		}
		else if (btn_pause.GetComponent<Image>().sprite.name == sprite_playing.name) {
			world.dino.GetComponent<PlayerInput>().enabled = false;
			timeScaleBuffer = Time.timeScale;
			Time.timeScale = 0.0f;
			btn_pause.GetComponent<Image>().sprite = sprite_paused;
			transform.GetChild(1).gameObject.SetActive(true);
			transform.GetChild(1).GetChild(1).GetComponent<Button>().interactable = true;
			transform.GetChild(1).GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = "Main Menu";
		}
	}

	public void ResumeGame() {
		btn_pause.GetComponent<Image>().sprite = sprite_playing;
		transform.GetChild(1).gameObject.SetActive(false);
		Time.timeScale = Mathf.Max(1.0f, timeScaleBuffer);
		world.dino.GetComponent<PlayerInput>().enabled = true;
	}

	public void RestartGame() {
		SceneManager.LoadScene(0);
	}

	public void QuitGame() {
		Application.Quit();
	}

	// Instructions Menu
	public void OpenInstructions() {
		transform.GetChild(3).gameObject.SetActive(true);
	}

	public void CloseInstructions() {
		transform.GetChild(3).gameObject.SetActive(false);
	}

	// Settings Menu
	public void OpenSettings() {
		transform.GetChild(4).gameObject.SetActive(true);
		transform.GetChild(4).GetChild(3).gameObject.SetActive(false);	// make sure the overlay isn't obscuring the menu
	}

	public void CloseSettings() {
		transform.GetChild(4).gameObject.SetActive(false);
	}

	public void SaveChanges() {
		PlayerPrefs.Save();
		StartCoroutine(SaveCoroutine());
	}

	public void ChangeGraphicsQuality(int option) {
		// 0 : high, 1 : medium, 2: low : 3
		TMP_Dropdown dropdown = transform.GetChild(4).GetChild(1).GetChild(0).GetChild(2).GetComponent<TMP_Dropdown>();
		if (dropdown.value >= 0 && dropdown.value < 4) {
			PlayerPrefs.SetInt("graphics_quality", dropdown.value);
		}

		SetQuality(dropdown.value);
	}

	public void ToggleShadows() {
		Button button = transform.GetChild(4).GetChild(1).GetChild(1).GetChild(2).GetComponent<Button>();
		ToggleOnOff(button);
		if (button.gameObject.transform.GetChild(0).GetComponent<TMP_Text>().text.ToLower() == "on") {
			PlayerPrefs.SetInt("shadows", 1);
			world.mainLight.shadows = LightShadows.Soft;
		}
		else {
			PlayerPrefs.SetInt("shadows", 0);
			world.mainLight.shadows = LightShadows.None;
		}
	}

	public void TogglePostPorcessing() {
		Button button = transform.GetChild(4).GetChild(1).GetChild(2).GetChild(2).GetComponent<Button>();
		ToggleOnOff(button);

		if (button.gameObject.transform.GetChild(0).GetComponent<TMP_Text>().text.ToLower() == "on") {
			PlayerPrefs.SetInt("post_processing", 1);
			world.postProcessVolume.SetActive(true);
		}
		else {
			PlayerPrefs.SetInt("post_processing", 0);
			world.postProcessVolume.SetActive(false);
		}
	}

	// gradually fades out an overlay image
	private IEnumerator SaveCoroutine() {
		Image settingsOverlay = transform.GetChild(4).GetChild(3).gameObject.GetComponent<Image>();
		settingsOverlay.gameObject.SetActive(true);
		settingsOverlay.color = new Color(settingsOverlay.color.r, settingsOverlay.color.g, settingsOverlay.color.b, 1.0f);

		for (float i = 1.0f; i > 0.0f; i -= 0.05f) {
			settingsOverlay.color = new Color(settingsOverlay.color.r, settingsOverlay.color.g, settingsOverlay.color.b, i);
			yield return new WaitForSecondsRealtime(0.05f);
		}

		settingsOverlay.color = new Color(settingsOverlay.color.r, settingsOverlay.color.g, settingsOverlay.color.b, 0.0f);
		settingsOverlay.gameObject.SetActive(false);
	}

	// utility methods
	private void ToggleOnOff(Button button) {
		TMP_Text text = button.gameObject.transform.GetChild(0).GetComponent<TMP_Text>();
		if (text.text.ToLower() == "on") {
			text.text = "off";
		}
		else if (text.text.ToLower() == "off") {
			text.text = "on";
		}
	}

	private void SetQuality(int level) {
		switch (level) {
			case 0:
				GraphicsSettings.renderPipelineAsset = world.high_quality_asset;
				QualitySettings.renderPipeline = world.high_quality_asset;
				break;
			case 1:
				GraphicsSettings.renderPipelineAsset = world.medium_quality_asset;
				QualitySettings.renderPipeline = world.medium_quality_asset;
				break;
			case 2:
				GraphicsSettings.renderPipelineAsset = world.low_quality_asset;
				QualitySettings.renderPipeline = world.low_quality_asset;
				break;
			default:
				break;
		}
	}
}