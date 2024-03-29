using UnityEngine;

using static Unity.Mathematics.math;

public class Player : MonoBehaviour
{
	[SerializeField, Min(0f)]
	float movementSpeed = 4f, rotationSpeed = 180f, mouseSensitivity = 5f;

	[SerializeField]
	float startingVerticalEyeAngle = 10f;

	CharacterController characterController;

	Transform eye;

	Vector2 eyeAngles;

	Camera eyeCamera;

	FieldOfView vision;

	public FieldOfView Vision => vision;

	void Awake()
	{
		characterController = GetComponent<CharacterController>();
		eye = transform.GetChild(0);
		eyeCamera = eye.GetComponent<Camera>();
		vision.range = 1000f;
	}

	public void StartNewGame(Vector3 position)
	{
		eyeAngles.x = Random.Range(0f, 360f);
		eyeAngles.y = startingVerticalEyeAngle;
		characterController.enabled = false;
		transform.localPosition = position;
		characterController.enabled = true;
	}

	public Vector3 Move()
	{
		UpdateEyeAngles();
		UpdatePosition();
		return transform.localPosition;
	}

	void UpdatePosition()
	{
		var movement = new Vector2(
			Input.GetAxis("Horizontal"),
			Input.GetAxis("Vertical"));
		float sqrMagnitude = movement.sqrMagnitude;
		if (sqrMagnitude > 1f)
		{
			movement /= Mathf.Sqrt(sqrMagnitude);
		}
		movement *= movementSpeed;

		var forward = new Vector2(
			Mathf.Sin(eyeAngles.x * Mathf.Deg2Rad),
			Mathf.Cos(eyeAngles.x * Mathf.Deg2Rad));
		var right = new Vector2(forward.y, -forward.x);

		movement = right * movement.x + forward * movement.y;
		characterController.SimpleMove(new Vector3(movement.x, 0f, movement.y));
	}

	void UpdateEyeAngles()
	{
		float rotationDelta = rotationSpeed * Time.deltaTime;
		eyeAngles.x += rotationDelta * Input.GetAxis("Horizontal View");
		eyeAngles.y -= rotationDelta * Input.GetAxis("Vertical View");
		if (mouseSensitivity > 0f)
		{
			float mouseDelta = rotationDelta * mouseSensitivity;
			eyeAngles.x += mouseDelta * Input.GetAxis("Mouse X");
			eyeAngles.y -= mouseDelta * Input.GetAxis("Mouse Y");
		}

		if (eyeAngles.x > 360f)
		{
			eyeAngles.x -= 360f;
		}
		else if (eyeAngles.x < 0f)
		{
			eyeAngles.x += 360f;
		}
		eyeAngles.y = Mathf.Clamp(eyeAngles.y, -45f, 45f);
		var rotation = eye.localRotation = Quaternion.Euler(eyeAngles.y, eyeAngles.x, 0f);

		float viewFactorY = Mathf.Tan(eyeCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		float viewFactorX = viewFactorY * eyeCamera.aspect;

		float y = eyeAngles.y < 0f ? viewFactorY : -viewFactorY;
		Vector3
			leftLine = rotation * new Vector3(-viewFactorX, y, 1f),
			rightLine = rotation * new Vector3(viewFactorX, y, 1f);
		vision.leftLine = float2(leftLine.x, leftLine.z);
		vision.rightLine = float2(rightLine.x, rightLine.z);
	}
}
