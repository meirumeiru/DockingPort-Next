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
		private ModuleDockingNode DockingNode = null;

//		private KFSMEvent on_deactivate = null;
//		private KFSMEvent on_activate = null;


		public enum Status { idle = 1, retracting = 2, extending = 3, extended = 4, captured = 5, uncaptured = 6, latched = 7, docking = 8, docked = 9, docked2 = 10 };

		// Ring

		private Status status = Status.idle;

		private Transform Ring;
		private Transform originalRingParent;
		private Vector3 originalRingLocalPosition;
		private Quaternion originalRingLocalRotation;

		private Transform[] aCylinder;
		private Transform[] aPiston;

		private GameObject RingObject;
		private ConfigurableJoint[] aPistonJoint;

		private float fExtendPosition = 0f;

		private ModuleDockingPortEx otherPort;

//		private ConfigurableJoint RingJoint;
		private ConfigurableJoint CaptureJoint;
		private Quaternion CaptureJointTargetRotation;
		private Vector3 CaptureJointTargetPosition;
		private Vector3 CaptureJointTargetPosition2;

		private bool RingJoint_ = false;			// FEHLER, kann auch gleich auf "CaptureJoint" prüfen

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
				GameEvents.onVesselDocking.Add(OnVesselDocking);
				GameEvents.onDockingComplete.Add(OnDockingComplete);
				GameEvents.onUndock.Add(OnUndock);
				GameEvents.onVesselsUndocking.Add(OnVesselsUndocking);
				GameEvents.onSameVesselDock.Add(OnSameVesselDock);
				GameEvents.onSameVesselUndock.Add(OnSameVesselUndock);
				GameEvents.onPartUndock.Add(OnPartUndock);
				GameEvents.onPartUndockComplete.Add(OnPartUndockComplete);
				GameEvents.onPartCouple.Add(OnPartCouple);
				GameEvents.onPartCoupleComplete.Add(OnPartCoupleComplete);
				GameEvents.onPartDeCouple.Add(OnPartDeCouple);
				GameEvents.onPartDeCoupleComplete.Add(OnPartDeCoupleComplete);
			}
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			// FEHLER, GetComponent? ginge das nicht?? *hmm*
			DockingNode = part.FindModuleImplementing<ModuleDockingNode>();

	//		DockingNode.Events["Undock"].VariantToggleEventDisabled(true);


			StartCoroutine(WaitAndInitialize());
		}

		public IEnumerator WaitAndInitialize()
		{
			while((DockingNode.fsm == null) || (!DockingNode.fsm.Started))
				yield return null;

			DockingNode.fsm.RunEvent(DockingNode.on_disable); // FEHLER, nur mal um etwas weiter zu kommen in der Entwicklung
/*
			on_deactivate = new KFSMEvent("Deactivate");
			on_deactivate.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_deactivate.GoToStateOnEvent = DockingNode.st_disabled;
			DockingNode.fsm.AddEvent(on_deactivate, new KFSMState[] { DockingNode.st_ready, DockingNode.st_disengage });

			on_activate = new KFSMEvent("Activate");
			on_activate.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_activate.GoToStateOnEvent = DockingNode.st_ready;
			DockingNode.fsm.AddEvent(on_activate, new KFSMState[] { DockingNode.st_disabled });
*/
			InitializeMeshes();

			AttachContextMenu();

			//			Events["TogglePort"].guiActive = true;

			UpdateUI();
		}

		public void OnDestroy()
		{
			DetachContextMenu();

			GameEvents.onVesselDocking.Remove(OnVesselDocking);
			GameEvents.onDockingComplete.Remove(OnDockingComplete);
			GameEvents.onUndock.Remove(OnUndock);
			GameEvents.onVesselsUndocking.Remove(OnVesselsUndocking);
			GameEvents.onSameVesselDock.Remove(OnSameVesselDock);
			GameEvents.onSameVesselUndock.Remove(OnSameVesselUndock);
			GameEvents.onPartUndock.Remove(OnPartUndock);
			GameEvents.onPartUndockComplete.Remove(OnPartUndockComplete);
			GameEvents.onPartCouple.Remove(OnPartCouple);
			GameEvents.onPartCoupleComplete.Remove(OnPartCoupleComplete);
			GameEvents.onPartDeCouple.Remove(OnPartDeCouple);
			GameEvents.onPartDeCoupleComplete.Remove(OnPartDeCoupleComplete);

			if(RingObject != null)
				RingObject.DestroyGameObject();
		}

		public void OnVesselDocking(uint oldId, uint newId)
		{
		}

		public void OnDockingComplete(GameEvents.FromToAction<Part, Part> partAction)
		{
		}

		public void OnUndock(EventReport e)
		{
		}

		public void OnVesselsUndocking(Vessel oldVessel, Vessel newVessel)
		{
		}

		public void OnSameVesselDock(GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode> e)
		{
		}

		public void OnSameVesselUndock(GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode> e)
		{
		}

		public void OnPartUndock(Part p)
		{
		}

		public void OnPartUndockComplete(Part p)
		{
		}

		public void OnPartCouple(GameEvents.FromToAction<Part, Part> partAction)
		{
		}

		public void OnPartCoupleComplete(GameEvents.FromToAction<Part, Part> partAction)
		{
		}

		public void OnPartDeCouple(Part p)
		{
		}

		public void OnPartDeCoupleComplete(Part p)
		{
		}

		////////////////////////////////////////
		// Functions

private void getch(Transform t, List<Transform> tl, string nm)
{
			for(int i = 0; i < t.childCount; i++)
			{
				Transform tc = t.GetChild(i);
				if(nm == tc.name)
					tl.Add(tc);
				else
					getch(tc, tl, nm);
			}
}

		static float ddd = 0.02f;
		static float eee = 0.01f;   // FEHLER, das ist die minimale Distanz ab wo der Ring reinziehen beginnt
		static int iii = 0;

		static float iiv = 10f; // FEHLER, Grund-Kraft für Drehungen     war 1
		static float iiv2 = 0.2f; // FEHLER, Grund-Kraft für Anziehung war 0.02
		static bool ahii = false;
		static bool ahii2 = true;

		static float f1 = 0.5f;
		static float f2 = 0.015f; // 0.002f, 0.008f ist zu wenig, 0.02 ist super schnell...
		static float damp0 = 0.002f;
		static float damp = 0.001f; // 0.0001 -> der Wechsel da drauf springt zu schnell...
		static float forc = 10000f;
		static int posC = 8; // 25 ist zu viel...
		static float f3 = 0.005f;

		private void InitializeMeshes()
		{
			Ring = KSPUtil.FindInPartModel(transform, "DD_Ring");

			originalRingParent = Ring.parent;
			originalRingLocalPosition = Ring.localPosition;
			originalRingLocalRotation = Ring.localRotation;

			aCylinder = new Transform[6];
			aPiston = new Transform[6];

			for(int i = 1; i < 7; i++)
			{
				aCylinder[i - 1] = KSPUtil.FindInPartModel(transform, "DD_Cyl" + i.ToString());
				aPiston[i - 1] = KSPUtil.FindInPartModel(transform, "DD_Piston" + i.ToString());

				aCylinder[i - 1].LookAt(aPiston[i - 1]);
				aPiston[i - 1].LookAt(aCylinder[i - 1]);
			}

// FEHLER, test
//return; // vorerst mal nicht mehr

			List<Transform> tl = new List<Transform>();

			Vector3 v = new Vector3(0f, 0f, -ddd);
			getch(transform, tl, "ColR");
			for(int i = 0; i < tl.Count; i++)
				tl[i].Translate(v, Space.Self);

	//		tl.Clear();

	//		v = new Vector3(0f, 0f, -ddd);
	//		getch(transform, tl, "ColP");			-> FEHLER, Richtung stimmt nicht
	//		for(int i = 0; i < tl.Count; i++)
	//			tl[i].Translate(v, Space.Self);
		}

		void BuildRingObject()
		{
			RingObject = new GameObject("DockingRing");

			RingObject.AddComponent<Rigidbody>().mass = 0.005f;

			RingObject.transform.position = transform.position;
			RingObject.transform.rotation = transform.rotation;

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
			float angle = 120f * p_iIndex;

			ConfigurableJoint joint = this.gameObject.AddComponent<ConfigurableJoint>();
			joint.connectedBody = RingObject.GetComponent<Rigidbody>();
		//	joint.autoConfigureConnectedAnchor = false;

			float distance = Math.Abs((transform.position - aCylinder[1].position).magnitude);

			Quaternion q = Quaternion.AngleAxis(angle, transform.right);
// FEHLER FEHLER, lokal nehmen!!
q = Quaternion.AngleAxis(angle, Vector3.up);

			Vector3 ttt =
				(aCylinder[1].position + aCylinder[2].position) / 2;

			Vector3 ttt2 =
				transform.InverseTransformPoint(ttt);

			Vector3 ttt3 =
				ttt2.normalized * (Math.Abs(transform.InverseTransformPoint(aCylinder[1].position).magnitude));

			Vector3 vv = Vector3.zero;
// FEHLER FEHLER, 0.1 mal lokale Achse nehmen

			joint.anchor =
				vv +
				q * ttt3;
//				joint.transform.InverseTransformPoint(
//				transform.TransformPoint(q * ttt3));
			// FEHLER, unnötig, ist das nicht genau das gleiche hier? also wie q * ttt3?

/*
			q = Quaternion.AngleAxis(angle, RingObject.transform.InverseTransformVector(transform.TransformVector(transform.right)));

			Vector3 uuu =
				(aPiston[1].position + aPiston[2].position) / 2;

			Vector3 uuu2 =
				RingObject.transform.InverseTransformPoint(uuu);

			Vector3 uuu3 =
				uuu2.normalized * (Math.Abs((aPiston[1].position - RingObject.transform.position).magnitude));


			joint.connectedAnchor =
//				joint.connectedBody.transform.InverseTransformPoint(
//			joint.connectedBody.transform.position + joint.connectedBody.transform.TransformDirection(q * uuu3));
q * uuu3;
*/

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


			joint.linearLimit = new SoftJointLimit() { limit = distance };

			joint.angularYLimit = new SoftJointLimit() { limit = 60f };
			joint.angularZLimit = new SoftJointLimit() { limit = 60f };


if(false)
{
			switch(p_iIndex)
			{
			case 0:
				DrawRelative(0, joint.transform.TransformPoint(joint.anchor), joint.transform.TransformVector(joint.axis));
				DrawRelative(4, joint.connectedBody.transform.TransformPoint(joint.connectedAnchor), joint.transform.TransformVector(joint.axis));
				break;

			case 1:
				DrawRelative(1, joint.transform.TransformPoint(joint.anchor), joint.transform.TransformVector(joint.axis));
				DrawRelative(5, joint.connectedBody.transform.TransformPoint(joint.connectedAnchor), joint.transform.TransformVector(joint.axis));
				break;

			case 2:
				DrawRelative(2, joint.transform.TransformPoint(joint.anchor), joint.transform.TransformVector(joint.axis));
				DrawRelative(6, joint.connectedBody.transform.TransformPoint(joint.connectedAnchor), joint.transform.TransformVector(joint.axis));
				break;
			}
}

			return joint;
		}

		private void BuildRingJoint(ModuleDockingPortEx port)
		{
			if(RingJoint_)
				return;


			RingObject.GetComponent<Rigidbody>().isKinematic = true;
			RingObject.GetComponent<Rigidbody>().detectCollisions = false;

			RingObject.transform.parent = port.transform;

			RingObject.transform.localRotation = new Quaternion(0f, 0f, 1f, 0f);
			RingObject.transform.localPosition = Vector3.zero;

RingJoint_ = true;

			otherPort = port;

			return;
// FEHLER, da drauf muss ich später
			RingObject.transform.localPosition = Vector3.zero;
			RingObject.transform.LookAt(-port.transform.forward);

			RingJoint_ = true;
/*
nope

			if(RingJoint != null)
				return;

			otherPort = port;

			ConfigurableJoint joint = RingObject.AddComponent<ConfigurableJoint>();
			joint.connectedBody = otherPort.GetComponent<Rigidbody>();

			joint.autoConfigureConnectedAnchor = false;

			joint.anchor = Vector3.zero;
			joint.connectedAnchor = Vector3.zero;

			joint.breakForce = joint.breakTorque = Mathf.Infinity;

			joint.xMotion = ConfigurableJointMotion.Free;
			joint.yMotion = ConfigurableJointMotion.Free;
			joint.zMotion = ConfigurableJointMotion.Free;
			joint.angularXMotion = ConfigurableJointMotion.Free;
			joint.angularYMotion = ConfigurableJointMotion.Free;
			joint.angularZMotion = ConfigurableJointMotion.Free;

			joint.rotationDriveMode = RotationDriveMode.XYAndZ;

			JointDrive drive = new JointDrive
			{
				positionSpring = 10f,
				positionDamper = 0.8f,
				maximumForce = 10f
			};

			joint.xDrive = drive;
			joint.yDrive = drive;
			joint.zDrive = drive;

		//	joint.angularXDrive = drive;
		//	joint.angularYZDrive = drive;

			RingJoint = joint;
*/
			status = Status.captured;

			Events["UnCapture"].guiActive = true;
			Events["RetractRing"].guiActive = false;
		}

		private void BuildCaptureJoint()
		{
			if(CaptureJoint != null)
				return;

			ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
			joint.connectedBody = otherPort.GetComponent<Rigidbody>();

//		joint.connectedAnchor = Vector3.zero;
//joint.anchor = joint.transform.InverseTransformPoint(joint.connectedBody.transform.position);

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

	//	joint.axis = transform.InverseTransformDirection(transform.forward);

			CaptureJoint = joint; // FEHLER, latchJoint nennen und auch die Funktion und so -> und den Ring evtl. Capture-Joint?

			iCapturePosition = -100;
_rotStep = 1f;
_transStep = 1f;
_trans = 1f;
		status = Status.captured;

			Events["RetractRing"].guiActive = false;
			Events["PerformDocking"].guiActive = true;

// event für's -> DockingNode aktivieren...
//jau... und beim docking, dann langsam reinziehen... mühsam, weil erst orientierung ausrichten,
//				dann reinziehen
//und same vessel docking sollte es nie geben

			CaptureJointTargetRotation =
				Quaternion.FromToRotation(CaptureJoint.transform.InverseTransformDirection(-otherPort.DockingNode.nodeTransform.forward), CaptureJoint.transform.InverseTransformDirection(DockingNode.nodeTransform.forward));

			CaptureJointTargetPosition =
				CaptureJoint.transform.InverseTransformDirection(-Vector3.ProjectOnPlane(otherPort.DockingNode.nodeTransform.position - DockingNode.nodeTransform.position, DockingNode.nodeTransform.forward));

			CaptureJointTargetPosition2 =
				CaptureJoint.transform.InverseTransformDirection(-Vector3.Project(otherPort.DockingNode.nodeTransform.position - DockingNode.nodeTransform.position, DockingNode.nodeTransform.forward));
		}

		ConfigurableJoint joint; Vector3 v5;

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = true, unfocusedRange = 2f, guiName = "Test")]
		public void Test()
		{
			if(status != Status.idle)
				return;

			BuildRingObject();

			RingObject.transform.Translate(Vector3.up, Space.Self);

			joint = gameObject.AddComponent<ConfigurableJoint>();
			joint.connectedBody = RingObject.GetComponent<Rigidbody>();

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
				positionDamper = 0.05f,
				maximumForce = 100f
			};

			joint.xDrive = drive;
			joint.yDrive = drive;
			joint.zDrive = drive;

			joint.slerpDrive = drive;

			//		joint.axis = transform.InverseTransformDirection(transform.forward);

			DrawRelative(0, transform.position, RingObject.transform.position - transform.position);

			v5 = Quaternion.AngleAxis(45, transform.forward) * transform.up;
			DrawRelative(3, transform.position, v5);

			//		joint.SetTargetRotationLocal(Quaternion.AngleAxis(45, joint.axis), Quaternion.identity);
			joint.targetRotation =
				//	Quaternion.Inverse(joint.transform.rotation) *
				Quaternion.FromToRotation(joint.transform.InverseTransformDirection(transform.up), joint.transform.InverseTransformDirection(v5));
		}

		int i7 = 0;
		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = true, unfocusedRange = 2f, guiName = "Test2")]
		public void Test2()
		{
			switch(i7)
			{
			case 0:
				joint.targetPosition = -0.5f * Vector3.up;
				break;

			case 1:
				joint.targetPosition = joint.transform.InverseTransformDirection(-0.5f * joint.transform.up);
				break;

			case 2:
				joint.targetPosition = joint.transform.InverseTransformDirection(-0.5f * transform.up);
				break;

			case 3:
				joint.targetPosition =
					joint.transform.InverseTransformDirection(
					Vector3.ProjectOnPlane(v5, transform.up));
				break;

			case 4:
				joint.targetPosition = joint.transform.InverseTransformDirection(
					-Vector3.ProjectOnPlane(v5, transform.up));
				break;
			}

			++i7;
			i7 %= 5;
		}


		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = true, unfocusedRange = 2f, guiName = "Extend Ring")]
		public void ExtendRing()
		{
			if(status != Status.idle)
				return;

			BuildRingObject();

			aPistonJoint = new ConfigurableJoint[3];

			aPistonJoint[0] = BuildPistonJoint(0);
			aPistonJoint[1] = BuildPistonJoint(1);
			aPistonJoint[2] = BuildPistonJoint(2);

			UpdateJoint(aPistonJoint[0]);
			UpdateJoint(aPistonJoint[1]);
			UpdateJoint(aPistonJoint[2]);

			status = Status.extending;

			Events["ExtendRing"].guiActive = false;
			Events["RetractRing"].guiActive = true;
		}

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = false, unfocusedRange = 2f, guiName = "Retract Ring")]
		public void RetractRing()
		{
			if((status != Status.extended) && (status != Status.extending) && (status != Status.uncaptured))
				return;

			status = Status.retracting;

			Events["RetractRing"].guiActive = false;
			Events["ExtendRing"].guiActive = true;
		}

		private void UpdatePistons()
		{
			for(int i = 0; i < 6; i++)
			{
				aCylinder[i].LookAt(aPiston[i]);
				aPiston[i].LookAt(aCylinder[i]);
			}
		}

		[KSPEvent(active = true, guiActive = false, guiActiveUnfocused = false, guiName = "uncapture")]
		public void UnCapture()
		{
			// FEHLER, prüfen, ob das ginge und so

			if(!RingJoint_)
				return;

			if(CaptureJoint != null)
				Destroy(CaptureJoint);

//			Destroy(RingJoint);
	// zurückumhängen und pistons aufbauen??

			status = Status.uncaptured;

			Events["UnCapture"].guiActive = false;
			Events["RetractRing"].guiActive = true;
		}

		[KSPEvent(active = true, guiActive = false, guiActiveUnfocused = false, guiName = "perform docking")]
		public void PerformDocking()
		{
			// FEHLER, prüfen, ob das ginge und so

			if(CaptureJoint == null)
				return;

			status = Status.docking;

// FEHLER, erster Versuch
Destroy(aPistonJoint[0]);
Destroy(aPistonJoint[1]);
Destroy(aPistonJoint[2]);

			Events["PerformDocking"].guiActive = false;
			Events["UnCapture"].guiActive = true;

			// abort docking? ... oder ist das uncapture? ... ja, vorerst -> evtl. stop? und dann fallen wir nach latched zurück? also einfach anhalten?
		}

		////////////////////////////////////////
		// Update-Functions

		public void FixedUpdate()
		{
			switch(status)
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

					status = Status.extended;
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

					status = Status.idle;

					UpdatePistons();
				}
				break;

			case Status.extended:

					// jetzt nach Partner scannen
					{
bool bfound = false;

for(int i = 0; i < FlightGlobals.VesselsLoaded.Count; i++)
{
	Vessel vessel = FlightGlobals.VesselsLoaded[i];

	if(vessel.packed)
		continue;

	for(int j = 0; j < vessel.dockingPorts.Count; j++)
	{
		PartModule partModule = vessel.dockingPorts[j];
		if(partModule.part == base.part)
			continue;

		if(partModule.part == null)
			continue;

		if(partModule.part.State == PartStates.DEAD)
			continue;

		ModuleDockingPortEx moduleDockingNode = partModule.GetComponent<ModuleDockingPortEx>();

		if(moduleDockingNode == null)
			continue;

	//	if(moduleDockingNode.status != Status.idle) // FEHLER, eigentlich eher "ready"... im Moment ist das nicht unterschieden
	//		continue;

		if(Math.Abs((moduleDockingNode.transform.position - RingObject.transform.position).magnitude) < 10f)
		{
			DockStatus = "gefunden";
								bfound = true;
		}

/*
DrawRelative(0, Ring.position, Ring.forward); // FEHLER, mal sehen halt
DrawRelative(1, RingObject.transform.position, RingObject.transform.forward); // FEHLER, mal sehen halt
DrawRelative(2, transform.position, transform.forward);
*/

DrawRelative(0, DockingNode.nodeTransform.position, DockingNode.nodeTransform.forward); // FEHLER, mal sehen halt
DrawRelative(1, moduleDockingNode.DockingNode.nodeTransform.position, moduleDockingNode.DockingNode.nodeTransform.forward); // FEHLER, mal sehen halt


						DockDistance = Math.Abs((moduleDockingNode.transform.position - RingObject.transform.position).magnitude).ToString();

		DockAngle = Vector3.Angle(RingObject.transform.up, moduleDockingNode.transform.up).ToString();

Vector3 d = moduleDockingNode.Ring.transform.position - Ring.transform.position;
d = Vector3.Project(d, Ring.forward);
		DockRingDistance = ((Vector3.Dot(d, Ring.forward) < 0) ? "-" : "") + Math.Abs(d.magnitude).ToString();

if(moduleDockingNode.RingObject != null)
{
	d = moduleDockingNode.RingObject.transform.position - RingObject.transform.position;
d = Vector3.Project(d, RingObject.transform.forward);

	DockRingDistanceO = ((Vector3.Dot(d, RingObject.transform.forward) < 0) ? "-" : "") + Math.Abs(d.magnitude).ToString();
}
else
		DockRingDistanceO = "-";

if(bfound)
{
	Vector3 relevantDistance = moduleDockingNode.Ring.transform.position - Ring.transform.position;
//	if(relevantDistance.magnitude <= eee)
//		BuildRingJoint(moduleDockingNode);


//	if(relevantDistance.magnitude <= 0.0001f)
	if(Vector3.Dot(d, Ring.forward) < 0) // FEHLER, neues Kriterium
	{
		// FEHLER, damit ich den "alten" Code nutzen kann... temp, aber... is ok jetzt mal... ok? gut...

		BuildRingJoint(moduleDockingNode);
/*										JointDrive drive = new JointDrive();
										drive.positionDamper = 100f;
										drive.positionSpring = 10000f;
										drive.maximumForce = 10000f;

										RingJoint.xDrive = drive;
										RingJoint.yDrive = drive;
										RingJoint.zDrive = drive;
*/
		BuildCaptureJoint();
	}
	
	if(relevantDistance.magnitude <= 0.5f) // <= eee war's
	{
	//	RingObject.GetComponent<Rigidbody>().AddTorque();


//		part.AddTorque(-(newPosition - position) * jointDamping * 0.001f * (Vector3d)GetAxis());
//										CollisionAnimationHandler der Distanz torque und force ausüben

//									kraft ausüben

/*
		DrawRelative(0, RingObject.transform.position, RingObject.transform.up);
		DrawRelative(1, moduleDockingNode.Ring.transform.position, moduleDockingNode.Ring.transform.forward);

		DrawRelative(2, RingObject.transform.position, RingObject.transform.right);
		DrawRelative(3, moduleDockingNode.Ring.transform.position, -moduleDockingNode.Ring.transform.right);
*/

	if(relevantDistance.magnitude <= 0.1f) // FEHLER, evtl. noch weniger? oder, erst wenn 'ne Force auf dem Joint ist? -> was ich nicht messen kann, glaub ich, aber evtl. könnte ich 'ne Distanz vom normalen weg erkennen?
	{
	float ff = 0.2f - (2 * relevantDistance.magnitude);
	//	ff = ff * ff;

	//RingObject.GetComponent<Rigidbody>().AddTorque(RingObject.transform.up + moduleDockingNode.Ring.transform.forward);
	//RingObject.GetComponent<Rigidbody>().AddTorque(-(RingObject.transform.up + moduleDockingNode.Ring.transform.forward));
	
								Quaternion rot =
	Quaternion.FromToRotation(RingObject.transform.up.normalized, -moduleDockingNode.Ring.transform.forward.normalized);
	RingObject.GetComponent<Rigidbody>().AddTorque(new Vector3(rot.x, rot.y, rot.z) * (ff * iiv));
		// bis 5 oder max 10 richtet das Zeugs komplett aus

	// jetzt das drehen hinbekommen

Vector3 myReference0 = RingObject.transform.right; float myRef0A = (Vector3.Angle(myReference0, -moduleDockingNode.Ring.transform.right));
Vector3 myReference1 = Quaternion.AngleAxis(120, RingObject.transform.up) *  RingObject.transform.right;
float myRef1A = (Vector3.Angle(myReference1, -moduleDockingNode.Ring.transform.right));
Vector3 myReference2 = Quaternion.AngleAxis(240, RingObject.transform.up) * RingObject.transform.right;
float myRef2A = (Vector3.Angle(myReference2, -moduleDockingNode.Ring.transform.right));

Vector3 myReference = myReference0; float myRefA = myRef0A;
if(myRef1A < myRefA) { myReference = myReference1; myRefA = myRef1A; }
if(myRef2A < myRefA) { myReference = myReference2; myRefA = myRef2A; }

									Quaternion rot2 =
	Quaternion.FromToRotation(
		Vector3.ProjectOnPlane(myReference, RingObject.transform.up).normalized, Vector3.ProjectOnPlane(-moduleDockingNode.Ring.transform.right, RingObject.transform.up).normalized);
// FEHLER, hier sehen, dass es zum nächsten 60er ginge
	RingObject.GetComponent<Rigidbody>().AddTorque(new Vector3(rot2.x, rot2.y, rot2.z) * (ff * iiv));


//		aPistonJoint[0].anchor zu connectedAnchor messen ??? -> weil, hab ja keine targetPosition -> dann, wenn verdreht das eine oder sonst das andere an Kraft aufwenden?

	// am Ende noch die Position

	RingObject.GetComponent<Rigidbody>().AddForce((moduleDockingNode.Ring.transform.position - RingObject.transform.position).normalized * (ff * iiv2));


	}

/*
up ist drehen, die anderen zwei sind ausrichten
											-> drehen müsste man, damit der Winkel stimmt

								die zwei ausrichter, damit er wieder gerade ist
	und die Kraft müsste kraft sein halt, damit das teil übereinstimmt mit dem wohin es muss

man sollte das ausrechnen anhand vom "wo wäre ich ohne Kraft" und em "wohin will ich"
											und dann... drehen wir und so anhand dem
											wie weit ich grade weg bin (nicht der ring,
												sondern der Port)
ja, sicher, das braucht noch etwas tuning, weil ich nicht weiss, wo das Zeug ist, aber
											-> linie zeichnen, dann versuchen drauf zu zeigen
											... das geht sicher
	... evtl. reichts, damit wir nur damit arbeiten könnten... also nur 1 joint statt 3?
				... mal sehen halt
*/

								/*
								switch(iii)
								{
								case 0:
									RingObject.GetComponent<Rigidbody>().AddTorque(iiv * RingObject.transform.up.normalized); break;
								case 1:
									RingObject.GetComponent<Rigidbody>().AddTorque(-iiv * RingObject.transform.up.normalized); break;
								case 2:
									RingObject.GetComponent<Rigidbody>().AddTorque(iiv * RingObject.transform.right.normalized); break;
								case 3:
									RingObject.GetComponent<Rigidbody>().AddTorque(-iiv * RingObject.transform.right.normalized); break;
								case 4:
									RingObject.GetComponent<Rigidbody>().AddTorque(iiv * RingObject.transform.forward.normalized); break;
								case 5:
									RingObject.GetComponent<Rigidbody>().AddTorque(-iiv * RingObject.transform.forward.normalized); break;
								}
								*/
							}

}
	}
}

if(!bfound)
	DockStatus = "-";
						//							suchen, dann versuchen einen capture zu bauen, wenn einer gefunden und nicht aktiv

					}
					break;

			case Status.captured:
				DockStatus = "captured";

				if(--iPos < 0)
				{
					++iCapturePosition;
					iPos = posC;
				}
				else
					return;

				{
					float f, d;

					if(iCapturePosition < 0)
					{
						f = Mathf.Max((iCapturePosition + 50) * 50f, 100f);
						d = damp0;
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
						status = Status.latched;

				}

/*				{
					Vector3 relevantDistance = otherPort.Ring.transform.position - Ring.transform.position;
					if(relevantDistance.magnitude <= 0.0001f)
						BuildCaptureJoint();
					else
					{
						JointDrive drive = new JointDrive();
						drive.positionDamper = 0.6f * RingJoint.xDrive.positionDamper;
						drive.positionSpring = 10f * RingJoint.xDrive.positionSpring;
						drive.maximumForce = 10f * RingJoint.xDrive.maximumForce;

						RingJoint.xDrive = drive;
						RingJoint.yDrive = drive;
						RingJoint.zDrive = drive;
					}
				}*/
				break;

			case Status.latched:
				DockStatus = "latched";
				break;

			case Status.docking:
				{
/*if(RingJoint)
	Destroy(RingJoint); // FEHLER, so ein Puff
if(RingObject)
	DestroyRingObject();
ok, das muss weg, sonst störts...*/

				//				DrawRelative(0, DockingNode.nodeTransform.position, DockingNode.nodeTransform.forward); // FEHLER, mal sehen halt
				//				DrawRelative(1, moduleDockingNode.DockingNode.nodeTransform.position, moduleDockingNode.DockingNode.nodeTransform.forward); // FEHLER, mal sehen halt

				float ang2 = Vector3.Angle(DockingNode.nodeTransform.forward, -otherPort.DockingNode.nodeTransform.forward);

				float ang = Vector3.Angle(
					CaptureJoint.anchor.normalized,
					CaptureJoint.transform.InverseTransformDirection(DockingNode.nodeTransform.forward).normalized);

				//DrawRelative(0, CaptureJoint.transform.position, CaptureJoint.transform.TransformDirection(CaptureJoint.anchor)); // FEHLER, mal sehen halt
				//DrawRelative(1, CaptureJoint.transform.position, DockingNode.nodeTransform.forward); // FEHLER, mal sehen halt


DrawRelative(1, DockingNode.nodeTransform.position, DockingNode.nodeTransform.forward);
DrawRelative(7, DockingNode.nodeTransform.position, -otherPort.DockingNode.nodeTransform.forward);

DrawRelative(2, DockingNode.nodeTransform.position, otherPort.DockingNode.nodeTransform.position - DockingNode.nodeTransform.position);

DrawRelative(4, DockingNode.nodeTransform.position, Vector3.ProjectOnPlane(otherPort.DockingNode.nodeTransform.position - DockingNode.nodeTransform.position, DockingNode.nodeTransform.forward));


float ang3 = Vector3.Angle(DockingNode.nodeTransform.forward, (otherPort.DockingNode.nodeTransform.position - DockingNode.nodeTransform.position).normalized);

//				if(ang > 0.5f) // halbes Grad
if((_rotStep > 0.01f) || (_transStep > 0.01f))
				{
Vector3 di =
CaptureJoint.transform.InverseTransformDirection(-Vector3.ProjectOnPlane(otherPort.DockingNode.nodeTransform.position - DockingNode.nodeTransform.position, DockingNode.nodeTransform.forward));

					DockStatus = "docking a2: " + ang2.ToString() + " d: " + di.magnitude;

// wieviel muss ich abzählen?
float qa = Quaternion.Angle(Quaternion.identity, CaptureJointTargetRotation);
		// qa / 0.008f; // so viele Schritte gibt's -> dann sind das 0.2° pro 25 Frames -> FEHLER, theoretisch Zeitschlitze beachten
_rotStep -= 1f / (qa / 0.008f);

if(_rotStep < 0f) _rotStep = 0f;

					CaptureJoint.targetRotation =
Quaternion.Slerp(CaptureJointTargetRotation, Quaternion.identity,
_rotStep);

float qd = CaptureJointTargetPosition.magnitude;
_transStep -= 1f / (qd / 0.0005f);

if(_transStep < 0f) _transStep = 0f;

//_transStep = Mathf.Min(f2 / di.magnitude, 1f);

					CaptureJoint.targetPosition =
Vector3.Slerp(CaptureJointTargetPosition, Vector3.zero,
_transStep);
				}
				else
				{
					DockStatus = "docking (retract)";

					CaptureJoint.targetRotation = CaptureJointTargetRotation;
					CaptureJoint.targetPosition = CaptureJointTargetPosition;


float qd = CaptureJointTargetPosition2.magnitude;
					_trans -= 1f / (qd / 0.0005f);

					if(_trans < 0f) _trans = 0f;

					//_transStep = Mathf.Min(f2 / di.magnitude, 1f);

					CaptureJoint.targetPosition +=
Vector3.Slerp(CaptureJointTargetPosition2, Vector3.zero,
_trans);

					if(_trans == 0f)
					{
						status = Status.docked;
						iPos = 10;
					}
				}

/*
				aufeinander zeigen (DictionaryEntry docking nodes

					if(winkel)
						eindrehen

					if(distanz)
						einziehen

					wenn alles gut -> dock auslösen*/
				}
				break;

			case Status.docked:
				DockStatus = "docked";

				if(--iPos < 0)
				{
					status = Status.docked2;
					DockingNode.fsm.RunEvent(DockingNode.on_enable);
					otherPort.DockingNode.fsm.RunEvent(otherPort.DockingNode.on_enable);
				}
					// FEHLER, hier jetzt warten... aber nur kurz, dann Dockingport anschalten und gut is
				break;
			}

			if(DockingNode.sameVesselDockJoint)
			{
				al[1].DrawLineInGameView(DockingNode.sameVesselDockJoint.transform.position,
					DockingNode.sameVesselDockJoint.transform.position + DockingNode.sameVesselDockJoint.transform.right, alColor[1]); // green

				al[2].DrawLineInGameView(DockingNode.sameVesselDockJoint.transform.position,
					DockingNode.sameVesselDockJoint.transform.position + DockingNode.sameVesselDockJoint.transform.forward, alColor[2]); // yellow

				al[3].DrawLineInGameView(DockingNode.sameVesselDockJoint.transform.position,
					DockingNode.sameVesselDockJoint.transform.position + DockingNode.sameVesselDockJoint.transform.up, alColor[3]); // magenta


				al[4].DrawLineInGameView(DockingNode.sameVesselDockJoint.Joint.transform.position + new Vector3(0.2f, 0, 0),
					DockingNode.sameVesselDockJoint.Joint.transform.position + new Vector3(0.2f, 0, 0) + DockingNode.sameVesselDockJoint.Joint.transform.right, alColor[4]); // blue

				al[5].DrawLineInGameView(DockingNode.sameVesselDockJoint.transform.position + new Vector3(0.2f, 0, 0),
					DockingNode.sameVesselDockJoint.Joint.transform.position + new Vector3(0.2f, 0, 0) + DockingNode.sameVesselDockJoint.Joint.transform.forward, alColor[5]); // white

				al[6].DrawLineInGameView(DockingNode.sameVesselDockJoint.Joint.transform.position + new Vector3(0.2f, 0, 0),
					DockingNode.sameVesselDockJoint.Joint.transform.position + new Vector3(0.2f, 0, 0) + DockingNode.sameVesselDockJoint.Joint.transform.up, alColor[6]); // türkis


				al[7].DrawLineInGameView(DockingNode.sameVesselDockJoint.Joint.transform.position + new Vector3(0.1f, 0, 0),
					DockingNode.sameVesselDockJoint.Joint.transform.position + new Vector3(0.1f, 0, 0) + DockingNode.sameVesselDockJoint.Joint.transform.TransformVector(DockingNode.sameVesselDockJoint.Joint.axis), alColor[7]); // grünlich

				al[8].DrawLineInGameView(DockingNode.sameVesselDockJoint.transform.position + new Vector3(0.1f, 0, 0),
					DockingNode.sameVesselDockJoint.Joint.transform.position + new Vector3(0.1f, 0, 0) + DockingNode.sameVesselDockJoint.Joint.transform.TransformVector(DockingNode.sameVesselDockJoint.Joint.secondaryAxis), alColor[8]); // dunkel-magenta


				al[9].DrawLineInGameView(DockingNode.sameVesselDockJoint.transform.position + new Vector3(0.3f, 0, 0),
					DockingNode.sameVesselDockJoint.Joint.transform.position + new Vector3(0.3f, 0, 0) + DockingNode.nodeTransform.forward, alColor[9]);

				al[10].DrawLineInGameView(DockingNode.sameVesselDockJoint.transform.position + new Vector3(0.3f, 0, 0),
					DockingNode.sameVesselDockJoint.Joint.transform.position + new Vector3(0.3f, 0, 0) - DockingNode.otherNode.nodeTransform.forward, alColor[10]);
			}
		}

		public void Update()
		{
if(DockingNode == null)
	DockingNode = part.FindModuleImplementing<ModuleDockingNode>(); // FEHLER, wenn der Port im Flug generiert wird (oder per KAS angehängt z.B.), dann... bekomm ich kein OnStart... -> klären was ich dann bekäme


return;

	//		UpdateUI(); -> evtl. zuviel des guten? oder aufteilen den Müll? ... *hmm* mal sehen
			DockStatus = DockingNode.fsm.currentStateName;

			if(DockingNode.otherNode == null)
			{
				DockDistance = "-";
				DockAngle = "-";
			}
			else
			{
				// ah... nodeTransform gibt das an :-) supi... gut zu wissen
				
				DockDistance = (DockingNode.nodeTransform.position - DockingNode.otherNode.nodeTransform.position).magnitude.ToString() + "m";
				DockAngle = Vector3.Angle(DockingNode.nodeTransform.forward, -DockingNode.otherNode.nodeTransform.forward).ToString() + "°";

				if(DockingNode.sameVesselDockJoint)
				{
					Vector3 posToNode = DockingNode.part.transform.position - DockingNode.nodeTransform.position;
					Vector3 r = -DockingNode.sameVesselDockJoint.transform.TransformVector(DockingNode.sameVesselDockJoint.HostAnchor); // in global jetzt
//der HostData anchor ist also was? die Position vom Host relativ zum nodeTransform -> posToNode == r gilt

					Vector3 posToJointPos = DockingNode.part.transform.position - DockingNode.sameVesselDockJoint.transform.position;
//ist das hier 0? glaube... oder? -> ja, ist 0


					// jetzt ist die Frage, wie das auf der anderen Seite aussieht... und dann hab ich dort einen Unterschied von posToNode zum r sag ich mal... also
					// der TgtAnchor ist falsch und muss angepasst werden... das ist der Trick... sag ich


/*

DockingNode.sameVesselDockJoint.
	und die blöde drehung müsste man auch noch hinbekommen... hmm... und das umhängen... na ja, nicht ganz einfach

er macht auf jeden Fall ConfigurableJoint's ... also, ist das gut so
	die werden verdreht aufgebaut und gelinkt... aber ist egal, Anker richtig = position stimmt und dann... rotation halt per target setzen...
		oder den Anker lassen und alles per Target bauen? ... *hmm* kann gehen, muss dann allerdings orgrot, orgpos updaten... das müsste man sich noch überlegen halt
*/


					Vector3 posToOtherNode = DockingNode.otherNode.part.transform.position - DockingNode.otherNode.nodeTransform.position;
					Vector3 s = -DockingNode.sameVesselDockJoint.transform.TransformVector(DockingNode.sameVesselDockJoint.TgtAnchor); // ist auch global
				// und das müsste wohin zeigen? auf den aktuellen Punkt wo das Teil war als gedockt wurde -> das ist aber falsch... sein müsste es wo?

					Vector3 sollPosvonOther = DockingNode.otherNode.part.transform.position + s; // das ist es, wohin ich zeigen müsste...
						// nein
					sollPosvonOther = DockingNode.otherNode.transform.position; // der hier muss auf mir liegen... das hier ist wohl das tgt-Anchor Teil
					// und das muss ich woanders hinziehen

					Vector3 daneben = DockingNode.otherNode.nodeTransform.position - DockingNode.nodeTransform.position; // um das liegen wir daneben

				//	es muss sich also was dahin ziehen oder sollPosvonOther...

	//				Vector3 anchorToNode = DockingNode.part.transform.position - DockingNode.sameVesselDockJoint.HostAnchor;
	//				Vector3 tgtAnchorToOtherNode = DockingNode.otherNode.part.transform.position  - DockingNode.sameVesselDockJoint.TgtAnchor;
				}
				else if(DockingNode.otherNode.sameVesselDockJoint)
				{
					Vector3 posToNode = DockingNode.part.transform.position - DockingNode.nodeTransform.position;
					Vector3 posToOtherNode = DockingNode.otherNode.part.transform.position - DockingNode.otherNode.nodeTransform.position;

					Vector3 anchorToNode = DockingNode.otherNode.part.transform.position - DockingNode.otherNode.sameVesselDockJoint.HostAnchor;
					Vector3 tgtAnchorToOtherNode = DockingNode.part.transform.position  - DockingNode.otherNode.sameVesselDockJoint.TgtAnchor;
				}
			}

//			Events["Undock"].active = DockingNode.Events["Undock"].active;
		}

		public void LateUpdate()
		{
			switch(status)
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


		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "X-Force", guiFormat = "F2",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 5f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.02f, maxValue = 5f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float forceX = 1f;

		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "YZ-Force", guiFormat = "F2",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 5f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.02f, maxValue = 5f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float forceYZ = 1f;

		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "X-Spring", guiFormat = "F2",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 5f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.02f, maxValue = 5f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float springX = 1f;

		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "YZ-Spring", guiFormat = "F2",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 5f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.02f, maxValue = 5f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float springYZ = 1f;

		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "X-Damp", guiFormat = "F2",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 5f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.02f, maxValue = 5f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float dampX = 0.05f;

		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "YZ-Damp", guiFormat = "F2",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 5f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.02f, maxValue = 5f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float dampYZ = 0.05f;


		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Angular-Force", guiFormat = "F2",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 5f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.02f, maxValue = 5f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float forceAngular = 0f;

		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Angular-Spring", guiFormat = "F2",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 5f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.02f, maxValue = 5f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float springAngular = 0f;

		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Angular-Damp", guiFormat = "F2",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 5f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.02f, maxValue = 5f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float dampAngular = 0f;


		void UpdateJoint(ConfigurableJoint j)
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

		private void onChanged_force(object o)
		{
			UpdateJoint(aPistonJoint[0]);
			UpdateJoint(aPistonJoint[1]);
			UpdateJoint(aPistonJoint[2]);
		}



		[KSPEvent(guiActive = false, guiActiveUnfocused = false, guiName = "würgi würgi", active = true)]
		public void Kacke()
		{
	//		var test = GameObject.CreatePrimitive(PrimitiveType.Cube);
			var test = new GameObject();
			if(test.GetComponent<Rigidbody>() == null)
				test.AddComponent<Rigidbody>();
			test.name = "theName";

/*
			DestroyImmediate(test.GetComponent<Collider>()); // yeah, why not...
			const float LOCAL_ANCHOR_DIM = 0.05f;
			test.transform.localScale = new Vector3(LOCAL_ANCHOR_DIM, LOCAL_ANCHOR_DIM, LOCAL_ANCHOR_DIM);
			var mr = test.GetComponent<MeshRenderer>();
			mr.name = test.name;
			mr.material = new Material(Shader.Find("Diffuse")) { color = Color.magenta };
*/
			test.GetComponent<Rigidbody>().mass = 0.005f; // ca. 5 kg
			Ring.parent = test.transform;
			Ring.localPosition = Vector3.zero;

			test.transform.position = transform.position + transform.up.normalized * 0.2f;


			test.SetActive(true);
			//test.GetComponent<Rigidbody>().WakeUp();


			RingObject = test;

			aPistonJoint = new ConfigurableJoint[3];

			aPistonJoint[0] = BuildPistonJoint(0);
			aPistonJoint[1] = BuildPistonJoint(1);
			aPistonJoint[2] = BuildPistonJoint(2);

			UpdateJoint(aPistonJoint[0]);
			UpdateJoint(aPistonJoint[1]);
			UpdateJoint(aPistonJoint[2]);



			return;

			// das zieht sofort ran... gut gut...
	//		DockingNode.sameVesselDockJoint.Joint.connectedAnchor = 
	//			DockingNode.otherNode.part.transform.InverseTransformPoint(
	//				DockingNode.otherNode.nodeTransform.position);



	//		DockingNode.sameVesselDockJoint.Joint.axis = DockingNode.sameVesselDockJoint.Joint.transform.right;
	//		DockingNode.sameVesselDockJoint.Joint.secondaryAxis = DockingNode.sameVesselDockJoint.Joint.transform.forward;

		/*

			DockingNode.sameVesselDockJoint.Joint.targetPosition =
				DockingNode.sameVesselDockJoint.Joint.transform.InverseTransformPoint(
					DockingNode.otherNode.nodeTransform.position);

			Vector3 v1 = DockingNode.otherNode.nodeTransform.position;
			Vector3 v2 = DockingNode.otherNode.nodeTransform.position - DockingNode.nodeTransform.position;
			Vector3 v3 = DockingNode.sameVesselDockJoint.Joint.transform.InverseTransformVector(v1);
			Vector3 v4 = DockingNode.sameVesselDockJoint.Joint.transform.InverseTransformVector(v2);

			Vector3 v = new Vector3(0f, 0f, 0f);

			switch(d)
			{
			case 0: break;
			case 1: v = v1; break;
			case 2: v = v2; break;
			case 3: v = v3; break;
			case 4: v = v4; break;
			}

			DockingNode.sameVesselDockJoint.Joint.targetPosition = v;

			Vector3	q = DockingNode.sameVesselDockJoint.Joint.targetPosition;

			if(d == 0)
			{
				DockingNode.sameVesselDockJoint.Joint.axis = new Vector3(1f, 0f, 0f);
				DockingNode.sameVesselDockJoint.Joint.secondaryAxis = new Vector3(0f, 1f, 0f);
			}
			else
			{
				DockingNode.sameVesselDockJoint.Joint.axis = DockingNode.sameVesselDockJoint.transform.right;
				DockingNode.sameVesselDockJoint.Joint.secondaryAxis = DockingNode.sameVesselDockJoint.transform.forward;
			}

	*/
//			if(d == 0)
if(true)
			{
				// perfekt
				DockingNode.sameVesselDockJoint.Joint.axis = new Vector3(1f, 0f, 0f);
				DockingNode.sameVesselDockJoint.Joint.secondaryAxis = new Vector3(0f, 1f, 0f);

				DockingNode.sameVesselDockJoint.Joint.rotationDriveMode = RotationDriveMode.Slerp;
				DockingNode.sameVesselDockJoint.Joint.slerpDrive = new JointDrive { maximumForce = PhysicsGlobals.JointForce, positionDamper = 0f, positionSpring = 0f };

				DockingNode.sameVesselDockJoint.Joint.angularXMotion = DockingNode.sameVesselDockJoint.Joint.angularYMotion = DockingNode.sameVesselDockJoint.Joint.angularZMotion = ConfigurableJointMotion.Free;

		//		DockingNode.sameVesselDockJoint.Joint.targetPosition = -DockingNode.sameVesselDockJoint.Joint.transform.InverseTransformVector(DockingNode.otherNode.nodeTransform.position - DockingNode.nodeTransform.position);
					// ist gut, aber ich will 'n Abstand um die Rotation besser zu sehen...

				// jetzt noch die Rotation...

				Vector3 forward = DockingNode.nodeTransform.forward;
				Vector3 right = Vector3.ProjectOnPlane(DockingNode.nodeTransform.right, forward);
				Vector3 up = Vector3.ProjectOnPlane(DockingNode.nodeTransform.up, forward);

				Vector3[] vs = { forward, right, up };

				Quaternion a, b = Quaternion.identity; float g;

				for(int i = 0; i < 3; i++)
				{
					a = Quaternion.Inverse(DockingNode.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(vs[i], vs[(i + 1) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(DockingNode.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(vs[i], vs[(i + 2) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(DockingNode.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(-vs[i], vs[(i + 1) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(DockingNode.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(-vs[i], vs[(i + 2) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(DockingNode.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(vs[i], -vs[(i + 1) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(DockingNode.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(vs[i], -vs[(i + 2) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(DockingNode.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(-vs[i], -vs[(i + 1) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(DockingNode.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(-vs[i], -vs[(i + 2) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;
				}

/*
				DockingNode.sameVesselDockJoint.Joint.targetRotation =
					Quaternion.Inverse(DockingNode.sameVesselDockJoint.Joint.transform.rotation) *
					Quaternion.LookRotation(
						Vector3.ProjectOnPlane(DockingNode.nodeTransform.right, -DockingNode.otherNode.nodeTransform.forward).normalized),
						Vector3.ProjectOnPlane(DockingNode.nodeTransform.up, -DockingNode.otherNode.nodeTransform.forward).normalized);
*/
				forward = -DockingNode.otherNode.nodeTransform.forward;

				forward = Quaternion.FromToRotation(forward, DockingNode.nodeTransform.forward) * DockingNode.nodeTransform.forward;

				right = Vector3.ProjectOnPlane(DockingNode.nodeTransform.right, forward);
				up = Vector3.ProjectOnPlane(DockingNode.nodeTransform.up, forward);

		//		Vector3[] vs = { forward, right, up };

				DockingNode.sameVesselDockJoint.Joint.targetRotation =
					Quaternion.Inverse(DockingNode.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(-up, forward);

			}
			else
			{
				// nur um zu sehen wohin das gehen soll
				DockingNode.sameVesselDockJoint.Joint.connectedAnchor = 
					DockingNode.otherNode.part.transform.InverseTransformPoint(
						DockingNode.otherNode.nodeTransform.position);
			}
		}

		////////////////////////////////////////
		// Properties

		////////////////////////////////////////
		// Status

		////////////////////////////////////////
		// Settings

		////////////////////////////////////////
		// Input

//		[KSPField(isPersistant = true)] public string forwardKey;

		////////////////////////////////////////
		// Editor

		////////////////////////////////////////
		// Context Menu

		private void AttachContextMenu()
		{
/*			if(HighLogic.LoadedSceneIsFlight)
			{
				Fields["_gui_minPositionLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_minPositionLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onMinPositionLimitChanged));

				Fields["_gui_maxPositionLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_maxPositionLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onMaxPositionLimitChanged));

				Fields["_gui_torqueLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_torqueLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onTorqueLimitChanged));

				Fields["_gui_accelerationLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_accelerationLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onAccelerationLimitChanged));

				Fields["_gui_speedLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_speedLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onSpeedLimitChanged));
			}
			else
			{
				Fields["_gui_minPositionLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_minPositionLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onMinPositionLimitChanged));

				Fields["_gui_maxPositionLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_maxPositionLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onMaxPositionLimitChanged));

				Fields["_gui_torqueLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_torqueLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onTorqueLimitChanged));

				Fields["_gui_accelerationLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_accelerationLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onAccelerationLimitChanged));

				Fields["_gui_speedLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_speedLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onSpeedLimitChanged));
			}*/
		}

		private void DetachContextMenu()
		{
/*			if(HighLogic.LoadedSceneIsFlight)
			{
				Fields["_gui_minPositionLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_minPositionLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onMinPositionLimitChanged));

				Fields["_gui_maxPositionLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_maxPositionLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onMaxPositionLimitChanged));

				Fields["_gui_torqueLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_torqueLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onTorqueLimitChanged));

				Fields["_gui_accelerationLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_accelerationLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onAccelerationLimitChanged));

				Fields["_gui_speedLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_speedLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onSpeedLimitChanged));
			}
			else if(HighLogic.LoadedSceneIsEditor)
			{
				Fields["_gui_minPositionLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_minPositionLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onMinPositionLimitChanged));

				Fields["_gui_maxPositionLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_maxPositionLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onMaxPositionLimitChanged));

				Fields["_gui_torqueLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_torqueLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onTorqueLimitChanged));

				Fields["_gui_accelerationLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_accelerationLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onAccelerationLimitChanged));

				Fields["_gui_speedLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_speedLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onSpeedLimitChanged));
			}*/
		}

		private void UpdateUI()
		{
			Fields["forceX"].OnValueModified += onChanged_force;
			Fields["forceYZ"].OnValueModified += onChanged_force;
			Fields["springX"].OnValueModified += onChanged_force;
			Fields["springYZ"].OnValueModified += onChanged_force;
			Fields["dampX"].OnValueModified += onChanged_force;
			Fields["dampYZ"].OnValueModified += onChanged_force;
			Fields["forceAngular"].OnValueModified += onChanged_force;
			Fields["springAngular"].OnValueModified += onChanged_force;
			Fields["dampAngular"].OnValueModified += onChanged_force;



//			Events["ContextMenuDisable"].active = (DockingNode.fsm.CurrentState == DockingNode.st_ready) || (DockingNode.fsm.CurrentState == DockingNode.st_disengage);
//			Events["ContextMenuDisable"].guiActive = (DockingNode.fsm.CurrentState == DockingNode.st_ready) || (DockingNode.fsm.CurrentState == DockingNode.st_disengage);

//			Events["ContextMenuEnable"].active = DockingNode.fsm.CurrentState == DockingNode.st_disabled;
//			Events["ContextMenuEnable"].guiActive = DockingNode.fsm.CurrentState == DockingNode.st_disabled;

//			Events["ContextMenuDecoupleAndDock"].active = DockingNode.fsm.CurrentState == DockingNode.st_preattached;
//			Events["ContextMenuDecoupleAndDock"].guiActive = DockingNode.fsm.CurrentState == DockingNode.st_preattached;

			DockStatus = DockingNode.fsm.currentStateName;

			Events["TogglePort"].guiName = (DockingNode.fsm.CurrentState == DockingNode.st_disabled) ? "Activate Port" : "Deactivate Port";
			Events["TogglePort"].active = false;

			DockingNode.Events["EnableXFeed"].active = false; // !DockingNode.crossfeed;
			DockingNode.Events["DisableXFeed"].active = false; // DockingNode.crossfeed;

			DockingNode.staged = false;

			DockingNode.Fields["acquireForceTweak"].guiActive = false;
			DockingNode.Fields["acquireForceTweak"].guiActiveEditor = false;

			/*			Events["InvertAxisToggle"].guiName = isInverted ? "Un-invert Axis" : "Invert Axis";
						Events["LockToggle"].guiName = isLocked ? "Disengage Lock" : "Engage Lock";

						if(canHaveLimits)
							Events["ToggleLimits"].guiName = hasPositionLimit ? "Disengage Limits" : "Engage Limits";

						if(HighLogic.LoadedSceneIsFlight)
						{
							Events["ToggleLimits"].guiActive = canHaveLimits;

							Fields["_gui_minPositionLimit"].guiActive = hasPositionLimit;
							Fields["_gui_maxPositionLimit"].guiActive = hasPositionLimit;

							((UI_FloatEdit)Fields["_gui_minPositionLimit"].uiControlFlight).minValue = (!isInverted ? minPosition : minPositionLimit);
							((UI_FloatEdit)Fields["_gui_minPositionLimit"].uiControlFlight).maxValue = (!isInverted ? maxPositionLimit : maxPosition);

							((UI_FloatEdit)Fields["_gui_maxPositionLimit"].uiControlFlight).minValue = (!isInverted ? minPositionLimit : minPosition);
							((UI_FloatEdit)Fields["_gui_maxPositionLimit"].uiControlFlight).maxValue = (!isInverted ? maxPosition : maxPositionLimit);

							Fields["_gui_torqueLimit"].guiActive = !isFreeMoving;
							((UI_FloatEdit)Fields["_gui_torqueLimit"].uiControlFlight).maxValue = maxTorque;

							Fields["_gui_accelerationLimit"].guiActive = !isFreeMoving;
							((UI_FloatEdit)Fields["_gui_accelerationLimit"].uiControlFlight).maxValue = maxAcceleration;

							Fields["_gui_speedLimit"].guiActive = !isFreeMoving;
							((UI_FloatEdit)Fields["_gui_speedLimit"].uiControlFlight).maxValue = maxSpeed;
						}
						else if(HighLogic.LoadedSceneIsEditor)
						{
							Events["ToggleLimits"].guiActiveEditor = canHaveLimits;

							Fields["_gui_minPositionLimit"].guiActiveEditor = hasPositionLimit;
							Fields["_gui_maxPositionLimit"].guiActiveEditor = hasPositionLimit;

							((UI_FloatEdit)Fields["_gui_minPositionLimit"].uiControlEditor).minValue = (!isInverted ? minPosition : minPositionLimit);
							((UI_FloatEdit)Fields["_gui_minPositionLimit"].uiControlEditor).maxValue = (!isInverted ? maxPositionLimit : maxPosition);

							((UI_FloatEdit)Fields["_gui_maxPositionLimit"].uiControlEditor).minValue = (!isInverted ? minPositionLimit : minPosition);
							((UI_FloatEdit)Fields["_gui_maxPositionLimit"].uiControlEditor).maxValue = (!isInverted ? maxPosition : maxPositionLimit);

							Fields["_gui_torqueLimit"].guiActiveEditor = !isFreeMoving;
							((UI_FloatEdit)Fields["_gui_torqueLimit"].uiControlEditor).maxValue = maxTorque;

							Fields["_gui_accelerationLimit"].guiActiveEditor = !isFreeMoving;
							((UI_FloatEdit)Fields["_gui_accelerationLimit"].uiControlEditor).maxValue = maxAcceleration;

							Fields["_gui_speedLimit"].guiActiveEditor = !isFreeMoving;
							((UI_FloatEdit)Fields["_gui_speedLimit"].uiControlEditor).maxValue = maxSpeed;

							Fields["jointSpring"].guiActiveEditor = hasSpring && isFreeMoving;
							Fields["jointDamping"].guiActiveEditor = hasSpring && isFreeMoving;

							Events["ActivateCollisions"].guiName = activateCollisions ? "Deactivate Collisions" : "Activate Collisions";
						}

						UIPartActionWindow[] partWindows = FindObjectsOfType<UIPartActionWindow>();
						foreach(UIPartActionWindow partWindow in partWindows)
						{
							if(partWindow.part == part)
								partWindow.displayDirty = true;
						}*/
		}

		[KSPField(guiName = "DockingNode status", isPersistant = false, guiActive = true)]
		public string DockStatus;

		[KSPField(guiName = "DockingNode distance", isPersistant = false, guiActive = true)]
		public string DockDistance;

		[KSPField(guiName = "DockingNode ring distance", isPersistant = false, guiActive = true)]
		public string DockRingDistance;

		[KSPField(guiName = "DockingNode ring distance O", isPersistant = false, guiActive = false)]
		public string DockRingDistanceO;

		[KSPField(guiName = "DockingNode angle", isPersistant = false, guiActive = false)]
		public string DockAngle;

		[KSPEvent(guiActive = true, guiActiveUnfocused = true, guiName = "Deactivate Port", active = true)]
		public void TogglePort()
		{
			if(DockingNode.fsm.CurrentState == DockingNode.st_disabled)
				DockingNode.fsm.RunEvent(DockingNode.on_enable);
			else
				DockingNode.fsm.RunEvent(DockingNode.on_disable);

			UpdateUI();
		}

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = true, unfocusedRange = 2f, guiName = "#autoLOC_6001445")]
		public void Undock()
		{
// FEHLER hier das evtl. nicht tun, wenn's ein zweit-teil noch gibt -> dann umhängen

			DockingNode.Undock();
		}

		////////////////////////////////////////
		// Actions

		[KSPAction("Extend")]
		public void Extend(KSPActionParam param)
		{ ExtendRing(); }

		[KSPAction("Retract")]
		public void Retract(KSPActionParam param)
		{ RetractRing(); }

// FEHLER? was?
/*		[KSPAction("Toggle Lock")]
		public void MotionLockToggle(KSPActionParam param)
		{ LockToggle(); }

		[KSPAction("Move To Next Preset")]
		public void MoveNextPresetAction(KSPActionParam param)
		{
			if(Presets != null)
				Presets.MoveNext();
		}
*/
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
