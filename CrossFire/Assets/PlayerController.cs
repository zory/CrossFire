using UnityEngine;

public class PlayerController : MonoBehaviour
{
	public float moveSpeed = 8f;
	public float turnSpeedDeg = 180f;

	void Update()
	{
		float turn = 0f;
		if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turn += 1f;
		if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turn -= 1f;

		float thrust = 0f;
		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) thrust += 1f;
		if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) thrust -= 1f;

		transform.Rotate(0f, 0f, turn * turnSpeedDeg * Time.deltaTime);
		transform.position += transform.up * (thrust * moveSpeed * Time.deltaTime);
	}
}