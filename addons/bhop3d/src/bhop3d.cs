using Godot;
using System;

public partial class PlayerAlt : CharacterBody3D
{
	[Export] public bool LookEnabled = true;

	// movement allowed or not
	[ExportCategory("Movement Allowed")]
	[Export] public bool MoveEnabled = true;
	[Export] public bool JumpWhenHeld = false;

	[ExportCategory("Mouse")]
	[Export] public float Sensitivity = 0.01f;
	private int CameraRotX;

	[ExportCategory("Movement Variables")]
	[Export] public float Gravity = 30f;
	[Export] public float GroundAccelerate = 250f; // ground acceleration
	[Export] public float AirAccelerate = 85f;
	[Export] public float MaxGroundVelocity = 10f;
	[Export] public float MaxAirVelocity = 1.5f;
	public float MaxVelocity;
	public float Acceleration;
	[Export] public float JumpForce = 1f;
	[Export] public float Friction = 6f;
	[Export] public static int BhopFrames = 2;
	public int FrameTimer = BhopFrames;

	[ExportCategory("Bunnyhopping")]
	[Export] public bool AdditiveBhop = true;

	[ExportCategory("Controlled Nodes")]
	[Export] public Camera3D Camera { get; set; }

	[ExportCategory("Debug")]
	// Whether to look for and update debug raycasts
	[Export] public bool DebugModeEnabled = false;
	// Raycast to update with wishdir
	[Export] public RayCast3D DebugWishdirRaycast { get; set; }
	// Raycast to update with velocity
	[Export] public RayCast3D DebugVelocityRaycast { get; set; }

	// Utility function for setting mouse mode, always visible if camera is unset
	public void UpdateMouseMode()
	{
		if (LookEnabled && IsInstanceValid(Camera))
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (LookEnabled && IsInstanceValid(Camera))
		{
			if (@event is InputEventMouseMotion mouseMotion)
			{
				var deltaX = mouseMotion.Relative.Y + Sensitivity;
				var deltaY = -mouseMotion.Relative.X + Sensitivity;

				RotateY(Mathf.DegToRad(deltaY));
				if (CameraRotX + deltaX > -89 && CameraRotX + deltaX < 89)
				{
					Camera.RotateX(Mathf.DegToRad(-deltaX));
					CameraRotX += (int)deltaX;
				}
			}
		}
	}

	public Vector3 GetWishdir()
	{
		if (!MoveEnabled)
		{
			return Vector3.Zero;
		}
		else
		{
			return Vector3.Zero + (Transform.Basis.Z * Input.GetAxis("forwards", "backwards")) + (Transform.Basis.X * Input.GetAxis("left", "right"));
		}
	}

	public float GetJump()
	{
		return Mathf.Sqrt(4 * JumpForce * Gravity);
	}

	public float GetGravity(float Delta)
	{
		return Gravity * Delta;
	}

	/*
		All this code was only possible thanks to the technical writeup by Flafla2 available below.
		Most of this was shamelessly adjusted from direct copy-pasting.
	
		Bunnyhopping from the Programmer's Perspective
		https://adrianb.io/2015/02/14/bunnyhop.html
	
		and i converted it from gdscript to C# (wildupe)
	*/

	// Source-like acceleration function
	public Vector3 Accelerate(Vector3 AccelDir, Vector3 PreviousVelocity, float Acceleration, float MaxVelocity, float delta)
	{
		float ProjectedVelocity = PreviousVelocity.Dot(AccelDir);
		float AccelerationVelocity = Mathf.Clamp(MaxVelocity - ProjectedVelocity, 0, Acceleration * delta);
		return PreviousVelocity + AccelDir * AccelerationVelocity;
	}

	public Vector3 GetNextVelocity(Vector3 PreviousVelocity, float delta)
	{
		bool Grounded = IsOnFloor();
		bool CanJump = Grounded;

		if (Grounded && FrameTimer >= BhopFrames)
		{
			var Speed = PreviousVelocity.Length();
			if (Speed != 0)
			{
				var Drop = Speed * Friction * delta;
				PreviousVelocity *= Mathf.Max(Speed - Drop, 0) / Speed;
			}
		}
		else
		{
			if(!AdditiveBhop)
			{
				Grounded = false;
			}
		}

		if(Grounded)
		{
			MaxVelocity = MaxGroundVelocity;
			Acceleration = GroundAccelerate;
		}
		else
		{
			MaxVelocity = MaxAirVelocity;
			Acceleration = AirAccelerate;
		}

		var Velocity = Accelerate(GetWishdir(), PreviousVelocity, Acceleration, MaxVelocity, delta);

		Velocity += Vector3.Down * GetGravity(delta);

		if (Input.IsActionJustPressed("jump") && MoveEnabled && CanJump)
		{
			Velocity.Y = GetJump();
		}

		return Velocity;
	}

	public void UpdateFrameTimer()
	{
		if (IsOnFloor())
		{
			FrameTimer+=1;
		}
		else
		{
			FrameTimer = 0;
		}
	}

	public void HandleMovement(double delta)
	{
		UpdateFrameTimer();
		Velocity = GetNextVelocity(Velocity, (float)delta);
		MoveAndSlide();
	}

	public void DrawDebug()
	{
		if (!DebugModeEnabled)
		{
			return;
		}

		var DebugVelocity = Velocity;
		DebugVelocity.Y = 0;

		GD.Print("Velocity = ", DebugVelocity.Length());

		if (IsInstanceValid(DebugVelocityRaycast))
		{
			DebugVelocityRaycast.TargetPosition = DebugVelocity;
		}

		if (IsInstanceValid(DebugWishdirRaycast))
		{
			DebugWishdirRaycast.TargetPosition = GetWishdir();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleMovement(delta);
		DrawDebug();
	}

	public override void _Ready()
	{
		UpdateMouseMode();
	}
}
