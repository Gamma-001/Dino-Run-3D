using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GlobalController : MonoBehaviour {
	[Header("Prefabs and Object References")]
	[SerializeField] private GameObject chunk;
	[SerializeField] private List<GameObject> obstacles;
	public DinoController dino;
	public UniversalRenderPipelineAsset high_quality_asset;
	public UniversalRenderPipelineAsset medium_quality_asset;
	public UniversalRenderPipelineAsset low_quality_asset;
	public Light mainLight;
	public GameObject postProcessVolume;

	[Header("Game Variables")]
	[SerializeField] [Range(1, 10)] private uint chunkCount = 3;
	[SerializeField] [Range(1.0f, 20.0f)] private float speed = 2.0f;
	[SerializeField] private float initialSpawnDistance = 6.0f;
	[SerializeField] private float fixedSpawnDistance = 5.0f;
	[SerializeField] private uint obstacleCount = 4;

	List<GameObject> chunks = new List<GameObject>();

	private Camera mainCam;
	private List<Stack<GameObject>> obstaclePool = new List<Stack<GameObject>>();
	private List<GameObject> activeObstacles = new List<GameObject>();
	private GameObject empty_pool;
	private GameObject empty_active;

	private float chunkLength;
	private float obstacleXpos;
	private float[] airObstacleYPos = new float[] { 0.4f, 0.625f, 1.0f };
	private uint scoreMultiplier = 25;

	private uint multiplierBuffer = 0;
	private float scoreBuffer = 0.0f;
	private static uint score = 0;
	public static uint Score => score;

	private void Start() {
		mainCam = Camera.main;
		obstacleXpos = dino.transform.position.x;
		empty_pool = new GameObject("Obstacle Pool");
		empty_active = new GameObject("Active Obstacles");

		// only works well if the prefab root scale is left to (1, 1, 1)
		chunkLength = Mathf.Abs(chunk.transform.localScale.z * chunk.transform.GetChild(1).localScale.z);

		// create the chunks
		// chunks are generated both ways from the origin, this also assumes that origin = geometric center
		for (int i = 0; i < chunkCount; i++) {
			float pos = chunkLength * (i - chunkCount / 2);
			GameObject temp = Instantiate(chunk);
			temp.transform.position = new Vector3(0.0f, 0.0f, pos);
			chunks.Add(temp);
		}

		// create the obstacles
		foreach (GameObject obstacle in obstacles) {
			Stack<GameObject> curPool = new Stack<GameObject>();
			for (int j = 0; j < obstacleCount + 1; j++) {
				GameObject temp = Instantiate(obstacle);
				temp.SetActive(false);
				temp.transform.parent = empty_pool.transform;
				curPool.Push(temp);
			}
			obstaclePool.Add(curPool);
		}

		for (int i = 0; i < obstacleCount; i++) {
			int index = Random.Range(0, obstacles.Count);
			GameObject temp = GetObstacleFromPool(index);
			float nextZpos = 0.0f;
			float yPos = 0.0f;
			if (i == 0) {
				nextZpos = dino.transform.position.z - initialSpawnDistance;
			}
			else {
				nextZpos = activeObstacles[activeObstacles.Count - 1].transform.position.z - fixedSpawnDistance;
			}
			if (temp.name.StartsWith("air")) {
				yPos = airObstacleYPos[Random.Range(0, airObstacleYPos.Length)];
			}

			temp.transform.position = new Vector3(obstacleXpos, yPos, nextZpos);
			temp.SetActive(true);
			temp.transform.parent = empty_active.transform;
			activeObstacles.Add(temp);
		}

		Time.timeScale = 1.0f;
		score = 0;
	}

	private void Update() {
		if (dino.GetComponent<Animator>().GetBool("Idling") == true) {
			return;
		}

		// update chunks
		// forward : z negative
		for (int i = 0; i < chunks.Count; i++) {
			chunks[i].transform.Translate(Vector3.forward * speed * Time.deltaTime);
			Vector2 screenPos = mainCam.WorldToScreenPoint(chunks[i].transform.GetChild(2).position);
			if (screenPos.x < 0.0f) {
				chunks[i].transform.Translate(Vector3.back * chunkLength * chunkCount);
			}
		}

		// update obstacles
		for (int i = 0; i < activeObstacles.Count; i++) {
			activeObstacles[i].transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.World);
			Vector2 screenPos = mainCam.WorldToScreenPoint(activeObstacles[i].transform.GetChild(2).position);
			if (screenPos.x < 0) {
				AddObstacleToPool(activeObstacles[i]);
				activeObstacles.RemoveAt(0);
				GameObject newObstacle = GetObstacleFromPool(Random.Range(0, obstacles.Count));
				float yPos = 0.0f;
				if (newObstacle.name.StartsWith("air")) {
					yPos = airObstacleYPos[Random.Range(0, airObstacleYPos.Length)];
				}

				newObstacle.transform.position = new Vector3(obstacleXpos, yPos, activeObstacles[activeObstacles.Count - 1].transform.position.z - fixedSpawnDistance);
				newObstacle.SetActive(true);
				newObstacle.transform.parent = empty_active.transform;
				activeObstacles.Add(newObstacle);
			}
		}

		scoreBuffer += scoreMultiplier * Time.deltaTime * Time.timeScale;
		if (scoreBuffer >= 1.0f) {
			score += (uint)Mathf.FloorToInt(scoreBuffer);
			multiplierBuffer += (uint)Mathf.FloorToInt(scoreBuffer);
			scoreBuffer = 0.0f;
		}

		if (multiplierBuffer > 30 && Time.timeScale < 3.0f) {
			Time.timeScale += 0.01f;
			multiplierBuffer = 0;
		}

		if (score >= 9999999) {
			score = 9999999;
		}
	}

	private void AddObstacleToPool(GameObject obstacle) {
		foreach (Stack<GameObject> x in obstaclePool) {
			if (x.Count != 0 && x.Peek().name == obstacle.name) {
				obstacle.SetActive(false);
				obstacle.transform.parent = empty_pool.transform;
				x.Push(obstacle);
				return;
			}
		}
	}

	private GameObject GetObstacleFromPool(int index) {
		if (index >= obstaclePool.Count) return null;

		return obstaclePool[index].Pop();
	}
}