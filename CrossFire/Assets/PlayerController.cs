using UnityEngine;

public class PlayerController : MonoBehaviour
{
	//public float moveSpeed = 8f;
	//public float turnSpeedDeg = 180f;

	//void Update()
	//{
	//	float turn = 0f;
	//	if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turn += 1f;
	//	if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turn -= 1f;

	//	float thrust = 0f;
	//	if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) thrust += 1f;
	//	if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) thrust -= 1f;

	//	transform.Rotate(0f, 0f, turn * turnSpeedDeg * Time.deltaTime);
	//	transform.position += transform.up * (thrust * moveSpeed * Time.deltaTime);
	//}

	public float thrustAccel = 25f;
	public float brakeAccel = 35f;
	public float maxSpeed = 60f;
	public float turnRate = 180f;

	[Header("Flight Assist")]
	public bool flightAssist = true;
	public float lateralDamping = 6f;

	Vector2 velocity;

	void Update()
	{
		float dt = Time.deltaTime;

		// Toggle Flight Assist
		if (Input.GetKeyDown(KeyCode.F))
			flightAssist = !flightAssist;

		// Rotation (Z axis)
		float turn = 0f;
		if (Input.GetKey(KeyCode.A)) turn += 1f;
		if (Input.GetKey(KeyCode.D)) turn -= 1f;
		transform.Rotate(0f, 0f, turn * turnRate * dt);

		Vector2 forward = transform.up;

		// Forward thrust (W)
		if (Input.GetKey(KeyCode.W))
			velocity += forward * (thrustAccel * dt);

		// Brake thrusters (S): reduce speed at brakeAccel m/s^2
		if (Input.GetKey(KeyCode.S))
		{
			float speed = velocity.magnitude;
			if (speed > 0.001f)
			{
				float newSpeed = Mathf.Max(0f, speed - brakeAccel * dt);
				velocity = velocity * (newSpeed / speed);
			}
		}

		// Flight Assist: remove sideways drift only
		if (flightAssist)
		{
			float forwardSpeed = Vector2.Dot(velocity, forward);
			Vector2 forwardVel = forward * forwardSpeed;
			Vector2 sidewaysVel = velocity - forwardVel;

			float k = Mathf.Exp(-lateralDamping * dt);
			sidewaysVel *= k;

			velocity = forwardVel + sidewaysVel;
		}

		// Hard max speed
		float v = velocity.magnitude;
		if (v > maxSpeed)
			velocity = velocity.normalized * maxSpeed;

		// Integrate position
		transform.position += (Vector3)(velocity * dt);
	}
}