using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;


namespace DockingPort_Next.Module
{
	public class ModuleDockingPortEx : PartModule
	{
		// Settings

		[KSPField(isPersistant = false), SerializeField]
		public float forceRotation = 10f; // basic force for rotation of the ring (a fake force for a smoother simulation)
		
		[KSPField(isPersistant = false), SerializeField]
		public float forceAttraction = 0.2f; // basic force for attraction of the ring (a fake force for a smoother simulation)

		[KSPField(isPersistant = false), SerializeField]
		public string ringName = "";

		[KSPField(isPersistant = false), SerializeField]
		public int pistonCount = 1;

		[KSPField(isPersistant = false), SerializeField]
		public Vector3 pistonVector = Vector3.zero;

		[KSPField(isPersistant = false), SerializeField]
		public Vector3 correctionVector = new Vector3(0f, 0.0817f, 0f);

		public struct LookAtInfo
		{
			public string partName;
			public string targetName;
			public Vector3 direction;
			public bool stretch;
		};

		public List<LookAtInfo> aLookAtInfo;

		public struct LookAt
		{
			public Transform part;
			public Transform target;
			public Vector3 direction;
			public bool stretch;
			public float factor;
		};

		public List<LookAt> aLookAt;

		// Docking and Status

		private ModuleDockingNode DockingNode = null;

		public enum Status { target = 0, idle = 1, retracting = 2, extending = 3, extended = 4, captured = 5, uncaptured = 6, latched = 7, docking = 8, predocked = 9, docked = 10 };

		// Ring

		private Status state = Status.idle;

		private Transform Ring = null;
		private Transform originalRingParent;
		private Vector3 originalRingLocalPosition;
		private Quaternion originalRingLocalRotation;
		private Vector3 relativeRingLocalDockingForward;
		private Vector3 relativeRingLocalDockingUp;

		private GameObject RingObject;
		private ConfigurableJoint[] aPistonJoint;

		private float fExtendPosition = 0f;

		private float detectionDistance = 5f;
		private ModuleDockingPortEx otherPort;

		private float captureDistance = 0.005f;

		private ConfigurableJoint CaptureJoint;

		private Vector3 originalRingObjectLocalPosition;
		private Quaternion originalRingObjectLocalRotation;

		private Quaternion CaptureJointTargetRotation;
		private Vector3 CaptureJointTargetPosition;
		private Vector3 CaptureJointTargetPositionRetracted;

		private float lastPreLatchDistance;

		private int iCapturePosition;
		private int iPos = 0;

		private float _rotStep = 0f;
		private float _transStep = 0f;
		private float _trans = 0f;

		////////////////////////////////////////
		// Constructor

		public ModuleDockingPortEx()
		{
			DebugInit();
		}

		////////////////////////////////////////
		// Callbacks

		public override void OnAwake()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				GameEvents.onPartCouple.Add(OnPartCouple);
				GameEvents.onPartUndockComplete.Add(OnPartUndockComplete);
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			LoadLookAt(node);
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			DockingNode = part.FindModuleImplementing<ModuleDockingNode>();

			DockingNode.acquireForce = 0f;
			DockingNode.acquireTorque = 0f;
			DockingNode.acquireTorqueRoll = 0f;

			DockingNode.undockEjectionForce = 0f;

			StartCoroutine(WaitAndInitialize());
		}

		public IEnumerator WaitAndInitialize()
		{
			while((DockingNode.fsm == null) || (!DockingNode.fsm.Started))
				yield return null;

			if(DockingNode.state.Contains("Docked"))
			{
				state = Status.docked;
				DockStatus = state.ToString();
			}
			else
				DockingNode.fsm.RunEvent(DockingNode.on_disable);

			InitializeMeshes();
			InitializeLookAt();

			AttachContextMenu();

			UpdateUI();
		}

		public void OnDestroy()
		{
			DetachContextMenu();

			GameEvents.onPartCouple.Remove(OnPartCouple);
			GameEvents.onPartUndockComplete.Remove(OnPartUndockComplete);

			if(RingObject != null)
				RingObject.DestroyGameObject();
		}

		public void OnPartCouple(GameEvents.FromToAction<Part, Part> partAction)
		{
			if((partAction.from == part) || (partAction.to == part))
			{
				if(CaptureJoint)
					Destroy(CaptureJoint);
			}
		}

		public void OnPartUndockComplete(Part p)
		{
			if((otherPort != null) && ((p == part) || (p == otherPort.part)))
			{
				state = Status.idle;
				DockStatus = state.ToString();

				Events["ExtendRing"].guiActive = true;

				otherPort.state = Status.idle;
				otherPort.DockStatus = otherPort.state.ToString();

				otherPort.Events["ExtendRing"].guiActive = true;

				StartCoroutine(WaitAndReinitialize());
			}
		}

		public IEnumerator WaitAndReinitialize()
		{
			while(DockingNode.state.Contains("Docked") || otherPort.DockingNode.state.Contains("Docked"))
				yield return null;

			DockingNode.fsm.RunEvent(DockingNode.on_disable);
			otherPort.DockingNode.fsm.RunEvent(otherPort.DockingNode.on_disable);
		}

		////////////////////////////////////////
		// Functions

		private void LoadLookAt(ConfigNode node)
		{
			if(aLookAtInfo == null)
			{
				if((part.partInfo != null) && (part.partInfo.partPrefab != null))
				{
					ModuleDockingPortEx prefabModule = (ModuleDockingPortEx)part.partInfo.partPrefab.Modules["ModuleDockingPortEx"];
					if(prefabModule != null)
					{
						aLookAtInfo = prefabModule.aLookAtInfo;
					}
				}
				else // I assume, that I'm the prefab then
				{
					aLookAtInfo = new List<LookAtInfo>();

					ConfigNode[] lookatnodes = node.GetNodes("LOOKAT");
					for(int i = 0; i < lookatnodes.Length; i++)
					{
						ConfigNode lookatnode = lookatnodes[i];

						LookAtInfo info = new LookAtInfo();

						lookatnode.TryGetValue("part", ref info.partName);
						lookatnode.TryGetValue("target", ref info.targetName);
						if(!lookatnode.TryGetValue("direction", ref info.direction))
							info.direction = Vector3.forward;
						if(!lookatnode.TryGetValue("stretch", ref info.stretch))
							info.stretch = false;

						aLookAtInfo.Add(info);
					}
				}
			}
		}

		private void InitializeLookAt()
		{
			aLookAt = new List<LookAt>(aLookAtInfo.Count);

			for(int i = 0; i < aLookAtInfo.Count; i++)
			{
				LookAtInfo info = aLookAtInfo[i];

				LookAt l = new LookAt();

				l.part = KSPUtil.FindInPartModel(part.transform, info.partName);
				l.target = KSPUtil.FindInPartModel(part.transform, info.targetName);
				l.direction = info.direction;
				l.stretch = info.stretch;
				if(l.stretch)
					l.factor = l.part.localScale.y / (l.target.position - l.part.position).magnitude;

				aLookAt.Add(l);
			}
		}

		private void InitializeMeshes()
		{
			if(Ring != null)
				return;

			Ring = KSPUtil.FindInPartModel(transform, ringName);

			originalRingParent = Ring.parent;
			originalRingLocalPosition = Ring.localPosition;
			originalRingLocalRotation = Ring.localRotation;

			relativeRingLocalDockingForward = Quaternion.Inverse(Ring.transform.rotation) * DockingNode.nodeTransform.forward;
			relativeRingLocalDockingUp = Quaternion.Inverse(Ring.transform.rotation) * DockingNode.nodeTransform.up;
		}

		void BuildRingObject()
		{
			RingObject = new GameObject("DockingRing");

			RingObject.AddComponent<Rigidbody>().mass = 0.005f;

			RingObject.transform.position = Ring.transform.position;
			RingObject.transform.rotation = Ring.transform.rotation;

			Ring.parent = RingObject.transform;

			RingObject.SetActive(true);
		}

		void DestroyRingObject()
		{
			Ring.parent = originalRingParent;

			Ring.localPosition = originalRingLocalPosition;
			Ring.localRotation = originalRingLocalRotation;

			RingObject.DestroyGameObject();
		}

		ConfigurableJoint BuildPistonJoint(int p_iIndex)
		{
			float angle = (360f / pistonCount) * p_iIndex;

			ConfigurableJoint joint = this.gameObject.AddComponent<ConfigurableJoint>();
			joint.connectedBody = RingObject.GetComponent<Rigidbody>();

			float distance = Math.Abs((transform.position - aLookAt[0].target.position).magnitude);

			Quaternion q = Quaternion.AngleAxis(angle, Vector3.up);

			Vector3 ttt =
				(aLookAt[0].target.position + aLookAt[1].target.position) / 2;

			Vector3 ttt2 =
				transform.InverseTransformPoint(ttt);

			Vector3 ttt3 =
				ttt2.normalized * (Math.Abs(transform.InverseTransformPoint(aLookAt[0].target.position).magnitude));

			ttt3 = pistonVector; // FEHLER, das ist neu jetzt

			Vector3 vv = Vector3.zero;
// FEHLER FEHLER, 0.1 mal lokale Achse nehmen

			joint.anchor =
				vv +
				q * ttt3;

		//	joint.axis = -joint.transform.right;
		//	joint.secondaryAxis = joint.transform.up;
			joint.axis = Vector3.up;
			joint.secondaryAxis = Vector3.right;


//			joint.targetPosition = Vector3.right * 0.3f;

			joint.breakForce = joint.breakTorque = Mathf.Infinity;

			joint.xMotion = ConfigurableJointMotion.Limited;
			joint.yMotion = ConfigurableJointMotion.Free;
			joint.zMotion = ConfigurableJointMotion.Free;
			joint.angularXMotion = ConfigurableJointMotion.Free;
			joint.angularYMotion = ConfigurableJointMotion.Limited;
			joint.angularZMotion = ConfigurableJointMotion.Limited;

			joint.rotationDriveMode = RotationDriveMode.XYAndZ;

// FEHLER, den Anchor in die mitte setzen
			joint.linearLimit = new SoftJointLimit() { limit = distance };

			joint.angularYLimit = new SoftJointLimit() { limit = 60f };
			joint.angularZLimit = new SoftJointLimit() { limit = 60f };

			UpdatePistonJoint(joint); // FEHLER, integrieren

//DrawRelative(p_iIndex + 1, joint.transform.TransformPoint(joint.anchor), joint.connectedBody.transform.TransformPoint(joint.connectedAnchor) - joint.transform.TransformPoint(joint.anchor));

			return joint;
		}

// FEHLER, aufräumen
		private float forceX = 1f;
		private float forceYZ = 1f;
		private float springX = 1f;
		private float springYZ = 1f;
		private float dampX = 0.05f;
		private float dampYZ = 0.05f;
		private float forceAngular = 0f;
		private float springAngular = 0f;
		private float dampAngular = 0f;

		void UpdatePistonJoint(ConfigurableJoint j) // FEHLER, das in die obere Funktion integrieren
		{
			j.angularYZDrive = new JointDrive
			{
				positionSpring = springAngular,
				positionDamper = dampAngular,
				maximumForce = forceAngular
			};

			JointDrive jdX = new JointDrive
			{
				positionSpring = springX,
				positionDamper = dampX,
				maximumForce = forceX
			};

			j.xDrive = jdX;

			JointDrive jdYZ = new JointDrive
			{
				positionSpring = springYZ,
				positionDamper = dampYZ,
				maximumForce = forceYZ
			};

			j.yDrive = j.zDrive = jdYZ;
		}

// FEHLER, aufräumen
		static float damp0 = 0.002f;
		static float damp = 0.001f;
		static float forc = 10000f;
		static int posC = 8;

		private void BuildCaptureJoint(ModuleDockingPortEx port)
		{
			otherPort = port;

			// RingObject
			RingObject.GetComponent<Rigidbody>().isKinematic = true;
			RingObject.GetComponent<Rigidbody>().detectCollisions = false;

			RingObject.transform.parent = port.transform;

			originalRingObjectLocalPosition = RingObject.transform.localPosition;
			originalRingObjectLocalRotation = RingObject.transform.localRotation;

		//	RingObject.transform.localPosition = Vector3.zero;
		//	RingObject.transform.localRotation = new Quaternion(0f, 0f, 1f, 0f);

// FEHLER, das da oben muss ich ändern, das mit der localPosition... hier probier ich mal was
			RingObject.transform.position =
				port.Ring.position + port.Ring.transform.TransformDirection(correctionVector);

	// snap local rotation
			
float newY = RingObject.transform.localEulerAngles.y;

newY -= (180f / pistonCount);
newY /= (360f / pistonCount);
newY = Mathf.Round(newY);
newY *= (360f / pistonCount);
newY += (180f / pistonCount);

			RingObject.transform.localEulerAngles = new Vector3(
				0f, newY, 180f);

			// Joint
			ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
			joint.connectedBody = otherPort.GetComponent<Rigidbody>();

			joint.breakForce = joint.breakTorque = Mathf.Infinity;

			joint.xMotion = ConfigurableJointMotion.Free;
			joint.yMotion = ConfigurableJointMotion.Free;
			joint.zMotion = ConfigurableJointMotion.Free;
			joint.angularXMotion = ConfigurableJointMotion.Free;
			joint.angularYMotion = ConfigurableJointMotion.Free;
			joint.angularZMotion = ConfigurableJointMotion.Free;

			joint.rotationDriveMode = RotationDriveMode.Slerp;

			JointDrive drive = new JointDrive
			{
				positionSpring = 100f,
				positionDamper = damp0,
				maximumForce = 100f
			};

			joint.xDrive = drive;
			joint.yDrive = drive;
			joint.zDrive = drive;

			joint.slerpDrive = drive;

			CaptureJoint = joint;

// FEHLER, die Modelle sind oft so ein elender Schrott... unglaublich du... -> geht's so???
joint.anchor = joint.transform.InverseTransformPoint(DockingNode.nodeTransform.position);

iCapturePosition = -100;

			CaptureJointTargetRotation =
				Quaternion.FromToRotation(CaptureJoint.transform.InverseTransformDirection(-otherPort.DockingNode.nodeTransform.forward), CaptureJoint.transform.InverseTransformDirection(DockingNode.nodeTransform.forward));
_rotStep = 1f;

			CaptureJointTargetPosition =
				CaptureJoint.transform.InverseTransformDirection(-Vector3.ProjectOnPlane(otherPort.DockingNode.nodeTransform.position - DockingNode.nodeTransform.position, DockingNode.nodeTransform.forward));
_transStep = 1f;

			CaptureJointTargetPositionRetracted =
				CaptureJoint.transform.InverseTransformDirection(-Vector3.Project(otherPort.DockingNode.nodeTransform.position - DockingNode.nodeTransform.position, DockingNode.nodeTransform.forward));
_trans = 1f;

			lastPreLatchDistance = (otherPort.DockingNode.nodeTransform.position - DockingNode.nodeTransform.position).magnitude;

			state = Status.captured;
			DockStatus = state.ToString();

			Events["RetractRing"].guiActive = false;
			Events["UnCapture"].guiActive = true;

			otherPort.state = Status.target;
			otherPort.DockStatus = otherPort.state.ToString();

			otherPort.Events["ExtendRing"].guiActive = false;
			otherPort.Events["RetractRing"].guiActive = false;
		}

		private void DestroyCaptureJoint()
		{
			// RingObject
			RingObject.transform.localPosition = originalRingObjectLocalPosition;
			RingObject.transform.localRotation = originalRingObjectLocalRotation;

			RingObject.transform.parent = null;

			RingObject.GetComponent<Rigidbody>().isKinematic = false;
			RingObject.GetComponent<Rigidbody>().detectCollisions = true;

			// Joint
			Destroy(CaptureJoint);

			state = Status.uncaptured;
			DockStatus = state.ToString();

			Events["UnCapture"].guiActive = false;
			Events["PerformDocking"].guiActive = false;
			Events["RetractRing"].guiActive = true;

			otherPort.state = Status.idle;
			otherPort.DockStatus = otherPort.state.ToString();

			otherPort.Events["ExtendRing"].guiActive = true;
		}

		[KSPEvent(guiActiveUnfocused = false, externalToEVAOnly = false, guiActive = true, unfocusedRange = 2f, guiName = "Extend Ring")] // FEHLER, evtl. doch nicht unfocused erlauben... raus damit?
		public void ExtendRing()
		{
			if((state != Status.idle) && (state != Status.retracting))
				return;

			if(RingObject == null)
				BuildRingObject();

			if(aPistonJoint == null)
			{
				aPistonJoint = new ConfigurableJoint[3];

				aPistonJoint[0] = BuildPistonJoint(0);
				aPistonJoint[1] = BuildPistonJoint(1);
				aPistonJoint[2] = BuildPistonJoint(2);
			}

			state = Status.extending;
			DockStatus = state.ToString();

			Events["ExtendRing"].guiActive = false;
			Events["RetractRing"].guiActive = true;
		}

		[KSPEvent(guiActiveUnfocused = false, externalToEVAOnly = false, guiActive = false, unfocusedRange = 2f, guiName = "Retract Ring")]
		public void RetractRing()
		{
			if((state != Status.extending) && (state != Status.extended) && (state != Status.uncaptured))
				return;

			state = Status.retracting;
			DockStatus = state.ToString();

			Events["RetractRing"].guiActive = false;
			Events["ExtendRing"].guiActive = true;
		}

		private void UpdatePistons()
		{
			for(int i = 0; i < aLookAt.Count; i++)
			{
				aLookAt[i].part.LookAt(aLookAt[i].target);
				aLookAt[i].part.rotation *= Quaternion.LookRotation(aLookAt[i].direction);
// FEHLER, noch stretch machen, wenn nötig

				if(aLookAt[i].stretch)
					aLookAt[i].part.localScale =
						new Vector3(1f, aLookAt[i].factor * (aLookAt[i].target.position - aLookAt[i].part.position).magnitude, 1f);
			}
		}

		[KSPEvent(active = true, guiActive = false, guiActiveUnfocused = false, guiName = "uncapture")]
		public void UnCapture()
		{
			DestroyCaptureJoint();
		}

		[KSPEvent(active = true, guiActive = false, guiActiveUnfocused = false, guiName = "perform docking")]
		public void PerformDocking()
		{
			Destroy(aPistonJoint[0]);
			Destroy(aPistonJoint[1]);
			Destroy(aPistonJoint[2]);

			aPistonJoint = null;

			state = Status.docking;
			DockStatus = state.ToString();

			Events["UnCapture"].guiActive = false;
			Events["PerformDocking"].guiActive = false;
			// OPTION: abort docking?
		}

		////////////////////////////////////////
		// Update-Functions

		public void FixedUpdate()
		{
			switch(state)
			{
			case Status.extending:
				if(fExtendPosition < 0.2f)
				{
					fExtendPosition += 0.005f;

					aPistonJoint[0].targetPosition = Vector3.right * fExtendPosition;
					aPistonJoint[1].targetPosition = Vector3.right * fExtendPosition;
					aPistonJoint[2].targetPosition = Vector3.right * fExtendPosition;
				}
				else
				{
					fExtendPosition = 0.2f;

					state = Status.extended;
					DockStatus = state.ToString();
				}
				break;

			case Status.retracting:
				if(fExtendPosition > 0f)
				{
					fExtendPosition -= 0.005f;

					aPistonJoint[0].targetPosition = Vector3.right * fExtendPosition;
					aPistonJoint[1].targetPosition = Vector3.right * fExtendPosition;
					aPistonJoint[2].targetPosition = Vector3.right * fExtendPosition;
				}
				else
				{
					fExtendPosition = 0f;

					Destroy(aPistonJoint[0]);
					Destroy(aPistonJoint[1]);
					Destroy(aPistonJoint[2]);

					aPistonJoint = null;

					DestroyRingObject();

					state = Status.idle;
					DockStatus = state.ToString();

					UpdatePistons();
				}
				break;

			case Status.extended:
				{
					ModuleDockingPortEx DockingNodeEx = null;
					Vector3 distance; float angle;

					for(int i = 0; (i < FlightGlobals.VesselsLoaded.Count) && !DockingNodeEx; i++)
					{
						Vessel vessel = FlightGlobals.VesselsLoaded[i];

						if(vessel.packed
						|| (vessel == part.vessel)) // no docking to ourself is possible
							continue;

						for(int j = 0; (j < vessel.dockingPorts.Count) && !DockingNodeEx; j++)
						{
							PartModule partModule = vessel.dockingPorts[j];

							if((partModule.part == null)
						//	|| (partModule.part == part) // no docking to ourself is possible
							|| (partModule.part.State == PartStates.DEAD))
								continue;

							ModuleDockingPortEx DockingNodeEx_ = partModule.GetComponent<ModuleDockingPortEx>();

							if(DockingNodeEx_ == null)
								continue;

							if(DockingNodeEx_.state != Status.idle)
								continue;

							distance = DockingNodeEx_.Ring.transform.position - RingObject.transform.position;

							if(distance.magnitude < detectionDistance)
							{
								DockDistance = distance.magnitude.ToString();

								angle = Vector3.Angle(DockingNode.nodeTransform.forward, -DockingNodeEx_.DockingNode.nodeTransform.forward);

								if((angle <= 15f) && (distance.magnitude <= 1f))
									DockingNodeEx = DockingNodeEx_;
							}
						}
					}

					if(DockingNodeEx)
					{
						DockStatus = "found";

						Rigidbody RingObjectBody = RingObject.GetComponent<Rigidbody>();

						float relevantDistance = (DockingNodeEx.Ring.transform.position - RingObject.transform.position).magnitude - correctionVector.magnitude;

						if(relevantDistance <= captureDistance)
						{
							BuildCaptureJoint(DockingNodeEx);
						}
						else if(relevantDistance < 0.1f)
						{
							float num = 0.2f - (2 * relevantDistance);

							Vector3 vector2 = Vector3.Cross(
								Ring.transform.rotation * relativeRingLocalDockingForward,
								-(DockingNodeEx.Ring.transform.rotation * DockingNodeEx.relativeRingLocalDockingForward));

							RingObjectBody.AddTorque(vector2 * num * forceRotation);

// anderer Weg wählen

Vector3 onPlane1 =
	Vector3.ProjectOnPlane(								
		Ring.transform.rotation * relativeRingLocalDockingUp,
		DockingNodeEx.Ring.transform.rotation * DockingNodeEx.relativeRingLocalDockingForward);

	float angle1 = Vector3.SignedAngle(onPlane1,
		-(DockingNodeEx.Ring.transform.rotation * DockingNodeEx.relativeRingLocalDockingUp),
		DockingNodeEx.Ring.transform.rotation * DockingNodeEx.relativeRingLocalDockingForward);

	angle1 /= (360f / pistonCount);
	angle1 = Mathf.Round(angle1);
	angle1 *= (360f / pistonCount);



	Vector3[] vectora3 = new Vector3[pistonCount];

	for(int i = 0; i < pistonCount; i++)
	{
		Quaternion ir = Quaternion.AngleAxis((360f / pistonCount) * i,
			DockingNodeEx.Ring.transform.rotation * DockingNodeEx.relativeRingLocalDockingForward); // FEHLER, optimierbar? ohne rotation und dafür ir * unten als 2. Faktor?

		vectora3[i] =
				Vector3.Cross(Ring.transform.rotation * relativeRingLocalDockingUp,
		-(ir * DockingNodeEx.Ring.transform.rotation * DockingNodeEx.relativeRingLocalDockingUp));
	}


	Vector3 vector3 = vectora3[0];
								for(int i = 1; i < pistonCount; i++)
									if(vector3.sqrMagnitude > vectora3[i].sqrMagnitude)
										vector3 = vectora3[i];

							RingObjectBody.AddTorque(vector3 * num * forceRotation);

							// push towards correct position
							RingObjectBody.AddForce((DockingNodeEx.Ring.transform.position - RingObject.transform.position).normalized * num * forceAttraction);
						}
					}
					else
						DockStatus = state.ToString();
				}
				break;

			case Status.captured:
				if(--iPos > 0)
					return;

				++iCapturePosition;
				iPos = posC;

				{
					float f, d;

					if(iCapturePosition < 0)
					{
						f = Mathf.Max((iCapturePosition + 50) * 50f, 100f);
						d = damp0;

						float preLatchDistance = (otherPort.DockingNode.nodeTransform.position - DockingNode.nodeTransform.position).magnitude;

						if(Mathf.Abs(preLatchDistance - lastPreLatchDistance) < 0.001f)
							iPos = 1;
						else
							lastPreLatchDistance = (2f * lastPreLatchDistance + preLatchDistance) / 3f;
					}
					else
					{
						f = forc * iCapturePosition;
						d = damp;
					}

					JointDrive drive = new JointDrive
					{
						positionSpring = f,
						positionDamper = d,
						maximumForce = f
					};

					CaptureJoint.xDrive = drive;
					CaptureJoint.yDrive = drive;
					CaptureJoint.zDrive = drive;

					CaptureJoint.slerpDrive = drive;

					if(iCapturePosition >= 25)
					{
						state = Status.latched;
						DockStatus = state.ToString();

						Events["PerformDocking"].guiActive = true;
					}
				}
				break;

			case Status.uncaptured:
				{
					float relevantDistance = (otherPort.Ring.transform.position - RingObject.transform.position).magnitude - correctionVector.magnitude;

					if(relevantDistance > 0.1f)
					{
						state = Status.extended;
						DockStatus = state.ToString();

						otherPort = null;
					}
				}
				break;

			case Status.latched:
				break;

			case Status.docking:
				{
					if((_rotStep > 0.01f) || (_transStep > 0.01f))
					{
						DockStatus = "docking (orientation)";

						_rotStep -= 1f / (Quaternion.Angle(Quaternion.identity, CaptureJointTargetRotation) / 0.008f);
						if(_rotStep < 0) _rotStep = 0f;

						_transStep -= 1f / (CaptureJointTargetPosition.magnitude / 0.0005f);
						if(_transStep < 0f) _transStep = 0f;

						CaptureJoint.targetRotation = Quaternion.Slerp(CaptureJointTargetRotation, Quaternion.identity, _rotStep);
						CaptureJoint.targetPosition = Vector3.Slerp(CaptureJointTargetPosition, Vector3.zero, _transStep);
					}
					else
					{
						DockStatus = "docking (retracting)";

						CaptureJoint.targetRotation = CaptureJointTargetRotation;

						_trans -= 1f / (CaptureJointTargetPositionRetracted.magnitude / 0.0005f);
						if(_trans < 0f)
						{
							_trans = 0f;
							state = Status.predocked;
							DockStatus = state.ToString();
							iPos = 10;
						}

						CaptureJoint.targetPosition = CaptureJointTargetPosition + Vector3.Slerp(CaptureJointTargetPositionRetracted, Vector3.zero, _trans);
					}
				}
				break;

			case Status.predocked:
				if(--iPos < 0)
				{
					DestroyRingObject();

					state = Status.docked;
					DockStatus = state.ToString();

					otherPort.state = Status.docked;
					otherPort.DockStatus = otherPort.state.ToString();

					DockingNode.fsm.RunEvent(DockingNode.on_enable);
					otherPort.DockingNode.fsm.RunEvent(otherPort.DockingNode.on_enable);

				//	Destroy(CaptureJoint); -> OnPartCouple
				}
				break;
			}
		}

		public void Update()
		{
			if(DockingNode == null)
				DockingNode = part.FindModuleImplementing<ModuleDockingNode>(); // FEHLER, wenn der Port im Flug generiert wird (oder per KAS angehängt z.B.), dann... bekomm ich kein OnStart... -> klären was ich dann bekäme
		}

		public void LateUpdate()
		{
			switch(state)
			{
			case Status.retracting:
			case Status.extending:
			case Status.extended:
			case Status.captured:
			case Status.uncaptured:
			case Status.latched:
			case Status.docking:
				UpdatePistons();
				break;
			}
		}

		////////////////////////////////////////
		// Context Menu

		private void AttachContextMenu()
		{
		}

		private void DetachContextMenu()
		{
		}

		private void UpdateUI()
		{
			Events["TogglePort"].guiName = (DockingNode.fsm.CurrentState == DockingNode.st_disabled) ? "Activate Port" : "Deactivate Port";
			Events["TogglePort"].active = false;

			DockingNode.Events["EnableXFeed"].active = false; // !DockingNode.crossfeed;
			DockingNode.Events["DisableXFeed"].active = false; // DockingNode.crossfeed;

			DockingNode.staged = false;

			DockingNode.Fields["acquireForceTweak"].guiActive = false;
			DockingNode.Fields["acquireForceTweak"].guiActiveEditor = false;
		}

		[KSPField(guiName = "DockingNode status", isPersistant = false, guiActive = true, guiActiveUnfocused = true, unfocusedRange = 20)]
		public string DockStatus = Status.idle.ToString();

		[KSPField(guiName = "DockingNode distance", isPersistant = false, guiActive = true)]
		public string DockDistance;

		[KSPEvent(guiActive = true, guiActiveUnfocused = true, guiName = "Deactivate Port", active = true)]
		public void TogglePort()
		{
			if(DockingNode.fsm.CurrentState == DockingNode.st_disabled)
				DockingNode.fsm.RunEvent(DockingNode.on_enable);
			else
				DockingNode.fsm.RunEvent(DockingNode.on_disable);

			UpdateUI();
		}

		////////////////////////////////////////
		// Actions

		[KSPAction("Extend")]
		public void Extend(KSPActionParam param)
		{ ExtendRing(); }

		[KSPAction("Retract")]
		public void Retract(KSPActionParam param)
		{ RetractRing(); }

		////////////////////////////////////////
		// Debug

		private LineDrawer[] al = new LineDrawer[13];
		private Color[] alColor = new Color[13];

		private String[] astrDebug;
		private int istrDebugPos;

		private void DebugInit()
		{
			for(int i = 0; i < 13; i++)
				al[i] = new LineDrawer();

			alColor[0] = Color.red;
			alColor[1] = Color.green;
			alColor[2] = Color.yellow;
			alColor[3] = Color.magenta;	// axis
			alColor[4] = Color.blue;	// secondaryAxis
			alColor[5] = Color.white;
			alColor[6] = new Color(33.0f / 255.0f, 154.0f / 255.0f, 193.0f / 255.0f);
			alColor[7] = new Color(154.0f / 255.0f, 193.0f / 255.0f, 33.0f / 255.0f);
			alColor[8] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 154.0f / 255.0f);
			alColor[9] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 255.0f / 255.0f);
			alColor[10] = new Color(244.0f / 255.0f, 238.0f / 255.0f, 66.0f / 255.0f);
			alColor[11] = new Color(244.0f / 255.0f, 170.0f / 255.0f, 66.0f / 255.0f); // orange
			alColor[12] = new Color(247.0f / 255.0f, 186.0f / 255.0f, 74.0f / 255.0f);

			astrDebug = new String[10240];
			istrDebugPos = 0;
		}

		private void DebugString(String s)
		{
			astrDebug[istrDebugPos] = s;
			istrDebugPos = (istrDebugPos + 1) % 10240;
		}

		private void DrawPointer(int idx, Vector3 p_vector)
		{
			al[idx].DrawLineInGameView(Vector3.zero, p_vector, alColor[idx]);
		}

// FEHLER, temp public, ich such was und brauch das als Anzeige
		public void DrawRelative(int idx, Vector3 p_from, Vector3 p_vector)
		{
			al[idx].DrawLineInGameView(p_from, p_from + p_vector, alColor[idx]);
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative, Vector3 p_off)
		{
			al[idx].DrawLineInGameView(p_transform.position + p_off, p_transform.position + p_off
				+ (p_relative ? p_transform.TransformDirection(p_vector) : p_vector), alColor[idx]);
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative)
		{ DrawAxis(idx, p_transform, p_vector, p_relative, Vector3.zero); }
	}
}
