using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;


namespace DockingPort_Next.Module
{
	/*
	 * This class helps ModuleDockingNode to do its job correctly. We cannot replace the ModuleDockingNode,
	 * because it is too deeply linked into KSPs code.
	 */

	public class ModuleDockingPortEx : PartModule
	{
		private ModuleDockingNode dock = null;

		private KFSMEvent on_deactivate = null;
		private KFSMEvent on_activate = null;


		public ModuleDockingPortEx()
		{
			DebugInit();
		}

		////////////////////////////////////////
		// Callbacks

		public override void OnAwake()
		{
		}
		
		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			// FEHLER, GetComponent? ginge das nicht?? *hmm*
			dock = part.FindModuleImplementing<ModuleDockingNode>();

/*
			GameEvents.onUndock.Add(onUndock);
			GameEvents.onVesselsUndocking.Add(onVesselsUndocking);
			GameEvents.onPartUndock.Add(onPartUndock);
			GameEvents.onPartUndockComplete.Add(onPartUndockComplete);
			GameEvents.onSameVesselDock.Add(onSameVesselDock);
			GameEvents.onSameVesselUndock.Add(onSameVesselUndock);
			GameEvents.onVesselDocking.Add(onVesselDocking);
			GameEvents.onDockingComplete.Add(onDockingComplete);
			GameEvents.onPartCouple.Add(onPartCouple);
			GameEvents.onPartCoupleComplete.Add(onPartCoupleComplete);
			GameEvents.onPartDeCouple.Add(onPartDeCouple);
			GameEvents.onPartDeCoupleComplete.Add(onPartDeCoupleComplete);
*/

	//		dock.Events["Undock"].VariantToggleEventDisabled(true);


	//		StartCoroutine(WaitAndInitialize());
			WaitAndInitialize();
		}

		public void onUndock(EventReport e)
		{
		}

		public void onVesselsUndocking(Vessel oldVessel, Vessel newVessel)
		{
		}

		public void onPartUndock(Part p)
		{
		}

		public void onPartUndockComplete(Part p)
		{
		}

		public void onSameVesselDock(GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode> e)
		{
			// es wurde ein Joint erstellt, aber wohl falsch... diesen jetzt umhängen von den Koordinaten her und langsam zusammenziehen lassen
		}

		public void onSameVesselUndock(GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode> e)
		{
		}

		public void onVesselDocking(uint oldId, uint newId)
		{
		}

		public void onDockingComplete(GameEvents.FromToAction<Part, Part> partAction)
		{
			// tja, hier und dort und überall noch den Status prüfen und fixen... weil er ja ab und zu (viel zu oft) kreuzfalsch ist am Ende von diesen Aktionen
		}

		public void onPartCouple(GameEvents.FromToAction<Part, Part> partAction)
		{
		}

		public void onPartCoupleComplete(GameEvents.FromToAction<Part, Part> partAction)
		{
		}

		public void onPartDeCouple(Part p)
		{
		}

		public void onPartDeCoupleComplete(Part p)
		{
		}

		public IEnumerator WaitAndInitialize()
		{
/*			if((dock.fsm == null) || (!dock.fsm.Started))
				yield return null;

			on_deactivate = new KFSMEvent("Deactivate");
			on_deactivate.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_deactivate.GoToStateOnEvent = dock.st_disabled;
			dock.fsm.AddEvent(on_deactivate, new KFSMState[] { dock.st_ready, dock.st_disengage });

			on_activate = new KFSMEvent("Activate");
			on_activate.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_activate.GoToStateOnEvent = dock.st_ready;
			dock.fsm.AddEvent(on_activate, new KFSMState[] { dock.st_disabled });
*/
			AttachContextMenu();

//			Events["TogglePort"].guiActive = true;

			UpdateUI();

			return null;
		}

		public void OnDestroy()
		{
			DetachContextMenu();
		}

		////////////////////////////////////////
		// Functions

		////////////////////////////////////////
		// Update-Functions

		public void FixedUpdate()
		{
			if(d != 0)
			{
				// FEHLER, ich rotier einfach selber mal

				Cyl4.LookAt(Piston4);
				Cyl5.LookAt(Piston5);

				Piston4.LookAt(Cyl4);
				Piston5.LookAt(Cyl5);


			}


			if(dock.sameVesselDockJoint)
			{
				al[1].DrawLineInGameView(dock.sameVesselDockJoint.transform.position,
					dock.sameVesselDockJoint.transform.position + dock.sameVesselDockJoint.transform.right, alColor[1]); // green

				al[2].DrawLineInGameView(dock.sameVesselDockJoint.transform.position,
					dock.sameVesselDockJoint.transform.position + dock.sameVesselDockJoint.transform.forward, alColor[2]); // yellow

				al[3].DrawLineInGameView(dock.sameVesselDockJoint.transform.position,
					dock.sameVesselDockJoint.transform.position + dock.sameVesselDockJoint.transform.up, alColor[3]); // magenta


				al[4].DrawLineInGameView(dock.sameVesselDockJoint.Joint.transform.position + new Vector3(0.2f, 0, 0),
					dock.sameVesselDockJoint.Joint.transform.position + new Vector3(0.2f, 0, 0) + dock.sameVesselDockJoint.Joint.transform.right, alColor[4]); // blue

				al[5].DrawLineInGameView(dock.sameVesselDockJoint.transform.position + new Vector3(0.2f, 0, 0),
					dock.sameVesselDockJoint.Joint.transform.position + new Vector3(0.2f, 0, 0) + dock.sameVesselDockJoint.Joint.transform.forward, alColor[5]); // white

				al[6].DrawLineInGameView(dock.sameVesselDockJoint.Joint.transform.position + new Vector3(0.2f, 0, 0),
					dock.sameVesselDockJoint.Joint.transform.position + new Vector3(0.2f, 0, 0) + dock.sameVesselDockJoint.Joint.transform.up, alColor[6]); // türkis


				al[7].DrawLineInGameView(dock.sameVesselDockJoint.Joint.transform.position + new Vector3(0.1f, 0, 0),
					dock.sameVesselDockJoint.Joint.transform.position + new Vector3(0.1f, 0, 0) + dock.sameVesselDockJoint.Joint.transform.TransformVector(dock.sameVesselDockJoint.Joint.axis), alColor[7]); // grünlich

				al[8].DrawLineInGameView(dock.sameVesselDockJoint.transform.position + new Vector3(0.1f, 0, 0),
					dock.sameVesselDockJoint.Joint.transform.position + new Vector3(0.1f, 0, 0) + dock.sameVesselDockJoint.Joint.transform.TransformVector(dock.sameVesselDockJoint.Joint.secondaryAxis), alColor[8]); // dunkel-magenta


				al[9].DrawLineInGameView(dock.sameVesselDockJoint.transform.position + new Vector3(0.3f, 0, 0),
					dock.sameVesselDockJoint.Joint.transform.position + new Vector3(0.3f, 0, 0) + dock.nodeTransform.forward, alColor[9]);

				al[10].DrawLineInGameView(dock.sameVesselDockJoint.transform.position + new Vector3(0.3f, 0, 0),
					dock.sameVesselDockJoint.Joint.transform.position + new Vector3(0.3f, 0, 0) - dock.otherNode.nodeTransform.forward, alColor[10]);
			}
		}

		public void Update()
		{
if(dock == null)
	dock = part.FindModuleImplementing<ModuleDockingNode>(); // FEHLER, wenn der Port im Flug generiert wird (oder per KAS angehängt z.B.), dann... bekomm ich kein OnStart... -> klären was ich dann bekäme


	//		UpdateUI(); -> evtl. zuviel des guten? oder aufteilen den Müll? ... *hmm* mal sehen
			DockStatus = dock.fsm.currentStateName;

			if(dock.otherNode == null)
			{
				DockDistance = "-";
				DockAngle = "-";
			}
			else
			{
				// ah... nodeTransform gibt das an :-) supi... gut zu wissen
				
				DockDistance = (dock.nodeTransform.position - dock.otherNode.nodeTransform.position).magnitude.ToString() + "m";
				DockAngle = Vector3.Angle(dock.nodeTransform.forward, -dock.otherNode.nodeTransform.forward).ToString() + "°";

				if(dock.sameVesselDockJoint)
				{
					Vector3 posToNode = dock.part.transform.position - dock.nodeTransform.position;
					Vector3 r = -dock.sameVesselDockJoint.transform.TransformVector(dock.sameVesselDockJoint.HostAnchor); // in global jetzt
//der HostData anchor ist also was? die Position vom Host relativ zum nodeTransform -> posToNode == r gilt

					Vector3 posToJointPos = dock.part.transform.position - dock.sameVesselDockJoint.transform.position;
//ist das hier 0? glaube... oder? -> ja, ist 0


					// jetzt ist die Frage, wie das auf der anderen Seite aussieht... und dann hab ich dort einen Unterschied von posToNode zum r sag ich mal... also
					// der TgtAnchor ist falsch und muss angepasst werden... das ist der Trick... sag ich


/*

dock.sameVesselDockJoint.
	und die blöde drehung müsste man auch noch hinbekommen... hmm... und das umhängen... na ja, nicht ganz einfach

er macht auf jeden Fall ConfigurableJoint's ... also, ist das gut so
	die werden verdreht aufgebaut und gelinkt... aber ist egal, Anker richtig = position stimmt und dann... rotation halt per target setzen...
		oder den Anker lassen und alles per Target bauen? ... *hmm* kann gehen, muss dann allerdings orgrot, orgpos updaten... das müsste man sich noch überlegen halt
*/


					Vector3 posToOtherNode = dock.otherNode.part.transform.position - dock.otherNode.nodeTransform.position;
					Vector3 s = -dock.sameVesselDockJoint.transform.TransformVector(dock.sameVesselDockJoint.TgtAnchor); // ist auch global
				// und das müsste wohin zeigen? auf den aktuellen Punkt wo das Teil war als gedockt wurde -> das ist aber falsch... sein müsste es wo?

					Vector3 sollPosvonOther = dock.otherNode.part.transform.position + s; // das ist es, wohin ich zeigen müsste...
						// nein
					sollPosvonOther = dock.otherNode.transform.position; // der hier muss auf mir liegen... das hier ist wohl das tgt-Anchor Teil
					// und das muss ich woanders hinziehen

					Vector3 daneben = dock.otherNode.nodeTransform.position - dock.nodeTransform.position; // um das liegen wir daneben

				//	es muss sich also was dahin ziehen oder sollPosvonOther...

	//				Vector3 anchorToNode = dock.part.transform.position - dock.sameVesselDockJoint.HostAnchor;
	//				Vector3 tgtAnchorToOtherNode = dock.otherNode.part.transform.position  - dock.sameVesselDockJoint.TgtAnchor;
				}
				else if(dock.otherNode.sameVesselDockJoint)
				{
					Vector3 posToNode = dock.part.transform.position - dock.nodeTransform.position;
					Vector3 posToOtherNode = dock.otherNode.part.transform.position - dock.otherNode.nodeTransform.position;

					Vector3 anchorToNode = dock.otherNode.part.transform.position - dock.otherNode.sameVesselDockJoint.HostAnchor;
					Vector3 tgtAnchorToOtherNode = dock.part.transform.position  - dock.otherNode.sameVesselDockJoint.TgtAnchor;
				}
			}

//			Events["Undock"].active = dock.Events["Undock"].active;
		}

int d = 0;

Transform Cyl4;
Transform Piston4;
Transform Cyl5;
Transform Piston5;
Transform Ring;

GameObject go;

ConfigurableJoint goJ1, goJ2, goJ3;


		ConfigurableJoint MakeJoint(int idx)
		{
			float deg = 0;
			if(idx == 1) deg = 120f;
			if(idx == 2) deg = 240f;

			ConfigurableJoint joint = this.gameObject.AddComponent<ConfigurableJoint>();
			joint.connectedBody = go.GetComponent<Rigidbody>();
joint.autoConfigureConnectedAnchor = false;

			float dist = Math.Abs((transform.position - Cyl4.position).magnitude);

			Quaternion q =
			Quaternion.AngleAxis(deg, transform.right);
			// FEHLER, prüfen, ob das ok ist oder 90° verdreht oder sowas

	//		joint.anchor = q * Vector3.right;

			Vector3 ttt =
				(Cyl4.position + Cyl5.position) / 2;

			Vector3 ttt2 =
				transform.InverseTransformPoint(ttt);

			Vector3 ttt3 =
				ttt2.normalized * (Math.Abs(transform.InverseTransformPoint(Cyl4.position).magnitude));

			// FEHLER, doof, weil ich mit cyl4 arbeite, womit ... das ganze Zeug dann doofe indexe hat... aber, egal jetzt

			joint.anchor = joint.transform.InverseTransformPoint(
				transform.TransformPoint(q * ttt3));
			// FEHLER, unnötig, ist das nicht genau das gleiche hier? also wie q * ttt3?



			//		joint.connectedAnchor = q * Vector3.right;
			// FEHLER, das hier gleich machen wie oben

			q = Quaternion.AngleAxis(deg, go.transform.InverseTransformVector(transform.TransformVector(transform.right)));


			Vector3 uuu =
				(Piston4.position + Piston5.position) / 2;

			Vector3 uuu2 =
				go.transform.InverseTransformPoint(uuu); // nope, wegen Scale geht's nicht
			uuu2 = go.transform.InverseTransformDirection(uuu - go.transform.position);

			Vector3 uuu3 =
				uuu2.normalized * (Math.Abs(go.transform.InverseTransformPoint(Piston4.position).magnitude)); // nope, wegen Scale geht's nicht
			uuu3 = uuu2.normalized * (Math.Abs((Piston4.position - go.transform.position).magnitude));

			//			joint.connectedAnchor = joint.connectedBody.transform.InverseTransformPoint(
			//				go.transform.TransformPoint(q * uuu3));
			// FEHLER, ist doch unnötig sowas, oder?

			joint.connectedAnchor =
				joint.connectedBody.transform.InverseTransformPoint(
			joint.connectedBody.transform.position + joint.connectedBody.transform.TransformDirection(q * uuu3)
				- (go.transform.position - transform.position));


			joint.axis = -joint.transform.right;
			joint.secondaryAxis = joint.transform.up;


			joint.targetPosition = Vector3.forward * 0.4f;


			//		Vector3 v1, v2, v3;
			//		v1 = originBody.transform.position - test.transform.position;
			//		v2 = Vector3.up;
			//		v3 = Vector3.right;

			//		Vector3.OrthoNormalize(ref v1, ref v2, ref v3);

			//		joint.axis = v1; joint.secondaryAxis = v2;


			joint.breakForce = joint.breakTorque = Mathf.Infinity;

			joint.xMotion = ConfigurableJointMotion.Limited;
			joint.yMotion = ConfigurableJointMotion.Free;
			joint.zMotion = ConfigurableJointMotion.Free;
			joint.angularXMotion = ConfigurableJointMotion.Free;
			joint.angularYMotion = ConfigurableJointMotion.Limited;
			joint.angularZMotion = ConfigurableJointMotion.Limited;

			joint.rotationDriveMode = RotationDriveMode.XYAndZ;
			/*joint.angularXDrive =*/ joint.angularYZDrive = new JointDrive
			{
				//mode = JointDriveMode.PositionAndVelocity,
				positionSpring = 0.1f,
				positionDamper = 0.1f,
				maximumForce = 0.1f
			};

			/*
							joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked; // wir haben rausgefunden, dass es das nicht braucht -> bzw. entweder das hier Free und die nachfolgenden Settings oder das hier Locked... -> weil am Grappler will ich ja nur die Drehung erlauben

							// ok, das hier würde, wäre alles "free" den Mist in der korrekten Position halten -> wäre also sowas wie eine "Feder" am Grappler
							joint.xMotion = ConfigurableJointMotion.Free;
			*/
			joint.xDrive =
				//	joint.yDrive = joint.zDrive =
				new JointDrive
				{
						//mode = JointDriveMode.PositionAndVelocity,
						positionSpring = 0.1f, // 10f,
						positionDamper = 0.1f, // 0.001f,
						maximumForce = 0.1f // 100f
				};

			joint.yDrive = joint.xDrive;
			joint.zDrive = joint.xDrive;


			SoftJointLimit sl = new SoftJointLimit();
			sl.limit = dist;

			joint.linearLimit = sl;

/*			sl = new SoftJointLimit();
			sl.limit = 160f;
			joint.highAngularXLimit = sl;

			sl = new SoftJointLimit();
			sl.limit = -160f;
			joint.lowAngularXLimit = sl;
*/
			sl = new SoftJointLimit();
			sl.limit = 160f;
			joint.angularYLimit = sl;


			/*
							joint.projectionAngle = 0f;
							joint.projectionDistance = 0f;
							//				joint.targetPosition = anchorPosition;
							//				joint.anchor = anchorPosition;



							joint.targetPosition = Vector3.right // move always along x axis!!
								* (originBody.transform.position - test.transform.position).magnitude; // / 2;


							// >>>>>> mit dem Schmarrn da oben erzeuge ich eigentlich nochmal ein Kettenglied... mal sehen wie's läuft

			*/


			if(deg < 30) // 0
			{
				DrawRelative(0, joint.transform.TransformPoint(joint.anchor), joint.transform.TransformVector(joint.axis));
				DrawRelative(4, joint.connectedBody.transform.TransformPoint(joint.connectedAnchor), joint.transform.TransformVector(joint.axis));

//				DrawRelative(9, joint.transform.position, joint.transform.InverseTransformPoint(Cyl4.position));//joint.transform.TransformVector(joint.axis));

//				DrawRelative(10, joint.connectedBody.transform.position, joint.connectedBody.transform.InverseTransformPoint(Piston4.position));//joint.transform.TransformVector(joint.axis));

//				DrawRelative(11, Vector3.zero, joint.connectedBody.transform.position);
//				DrawRelative(12, Vector3.zero, Piston4.position);

			}
			else if(deg < 150) // 1
			{
				DrawRelative(1, joint.transform.TransformPoint(joint.anchor), joint.transform.TransformVector(joint.axis));
				DrawRelative(5, joint.connectedBody.transform.TransformPoint(joint.connectedAnchor), joint.transform.TransformVector(joint.axis));
			}
			else
			{
				DrawRelative(2, joint.transform.TransformPoint(joint.anchor), joint.transform.TransformVector(joint.axis));
				DrawRelative(6, joint.connectedBody.transform.TransformPoint(joint.connectedAnchor), joint.transform.TransformVector(joint.axis));
			}

			return joint;
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "X-Force", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 10f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.1f, maxValue = 10f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float forceX = 0.1f;

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "YZ-Force", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 10f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.1f, maxValue = 10f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float forceYZ = 0.1f;

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "X-Spring", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 10f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.1f, maxValue = 10f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float springX = 0.1f;

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "YZ-Spring", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 10f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.1f, maxValue = 10f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float springYZ = 0.1f;

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "X-Damp", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 10f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.1f, maxValue = 10f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float dampX = 0.1f;

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "YZ-Damp", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 10f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.1f, maxValue = 10f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float dampYZ = 0.1f;


		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Angular-Force", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 10f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.1f, maxValue = 10f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float forceAngular = 0.1f;

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Angular-Spring", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 10f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.1f, maxValue = 10f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float springAngular = 0.1f;

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Angular-Damp", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 10f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.1f, maxValue = 10f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float dampAngular = 0.1f;


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
			if(d == 0)
				return;

			UpdateJoint(goJ1);
			UpdateJoint(goJ2);
			UpdateJoint(goJ3);
		}



		[KSPEvent(guiActive = true, guiActiveUnfocused = false, guiName = "würgi würgi", active = true)]
		public void Kacke()
		{
			if(d != 0)
				return;


			Cyl4 = KSPUtil.FindInPartModel(transform, "DD_Cyl4");
			Piston4 = KSPUtil.FindInPartModel(transform, "DD_Piston4");
			Cyl5 = KSPUtil.FindInPartModel(transform, "DD_Cyl5");
			Piston5 = KSPUtil.FindInPartModel(transform, "DD_Piston5");

			Ring = KSPUtil.FindInPartModel(transform, "DD_Ring");




			var test = GameObject.CreatePrimitive(PrimitiveType.Cube);
			if(test.GetComponent<Rigidbody>() == null)
				test.AddComponent<Rigidbody>();
			test.name = "theName";
			DestroyImmediate(test.GetComponent<Collider>()); // yeah, why not...
			const float LOCAL_ANCHOR_DIM = 0.05f;
			test.transform.localScale = new Vector3(LOCAL_ANCHOR_DIM, LOCAL_ANCHOR_DIM, LOCAL_ANCHOR_DIM);
			var mr = test.GetComponent<MeshRenderer>();
			mr.name = test.name;
			mr.material = new Material(Shader.Find("Diffuse")) { color = Color.magenta };
			test.GetComponent<Rigidbody>().mass = 0.1f;
	Rigidbody rb = test.GetComponent<Rigidbody>();
	// FEHLER; ist gut so, reagiert aber nicht auf Gravitation... ist mir aber egal

			Ring.parent = test.transform;
			Ring.localPosition = Vector3.zero;


			test.transform.position = transform.position + transform.up.normalized * 0.2f;


			test.SetActive(true);
//test.GetComponent<Rigidbody>().WakeUp();


			go = test;

			goJ1 = MakeJoint(0);
			goJ2 = MakeJoint(1);
			goJ3 = MakeJoint(2);



				d = 1;
			return;

			// das zieht sofort ran... gut gut...
	//		dock.sameVesselDockJoint.Joint.connectedAnchor = 
	//			dock.otherNode.part.transform.InverseTransformPoint(
	//				dock.otherNode.nodeTransform.position);



	//		dock.sameVesselDockJoint.Joint.axis = dock.sameVesselDockJoint.Joint.transform.right;
	//		dock.sameVesselDockJoint.Joint.secondaryAxis = dock.sameVesselDockJoint.Joint.transform.forward;

		/*

			dock.sameVesselDockJoint.Joint.targetPosition =
				dock.sameVesselDockJoint.Joint.transform.InverseTransformPoint(
					dock.otherNode.nodeTransform.position);

			Vector3 v1 = dock.otherNode.nodeTransform.position;
			Vector3 v2 = dock.otherNode.nodeTransform.position - dock.nodeTransform.position;
			Vector3 v3 = dock.sameVesselDockJoint.Joint.transform.InverseTransformVector(v1);
			Vector3 v4 = dock.sameVesselDockJoint.Joint.transform.InverseTransformVector(v2);

			Vector3 v = new Vector3(0f, 0f, 0f);

			switch(d)
			{
			case 0: break;
			case 1: v = v1; break;
			case 2: v = v2; break;
			case 3: v = v3; break;
			case 4: v = v4; break;
			}

			dock.sameVesselDockJoint.Joint.targetPosition = v;

			Vector3	q = dock.sameVesselDockJoint.Joint.targetPosition;

			if(d == 0)
			{
				dock.sameVesselDockJoint.Joint.axis = new Vector3(1f, 0f, 0f);
				dock.sameVesselDockJoint.Joint.secondaryAxis = new Vector3(0f, 1f, 0f);
			}
			else
			{
				dock.sameVesselDockJoint.Joint.axis = dock.sameVesselDockJoint.transform.right;
				dock.sameVesselDockJoint.Joint.secondaryAxis = dock.sameVesselDockJoint.transform.forward;
			}

	*/
			if(d == 0)
			{
				// perfekt
				dock.sameVesselDockJoint.Joint.axis = new Vector3(1f, 0f, 0f);
				dock.sameVesselDockJoint.Joint.secondaryAxis = new Vector3(0f, 1f, 0f);

				dock.sameVesselDockJoint.Joint.rotationDriveMode = RotationDriveMode.Slerp;
				dock.sameVesselDockJoint.Joint.slerpDrive = new JointDrive { maximumForce = PhysicsGlobals.JointForce, positionDamper = 0f, positionSpring = 0f };

				dock.sameVesselDockJoint.Joint.angularXMotion = dock.sameVesselDockJoint.Joint.angularYMotion = dock.sameVesselDockJoint.Joint.angularZMotion = ConfigurableJointMotion.Free;

		//		dock.sameVesselDockJoint.Joint.targetPosition = -dock.sameVesselDockJoint.Joint.transform.InverseTransformVector(dock.otherNode.nodeTransform.position - dock.nodeTransform.position);
					// ist gut, aber ich will 'n Abstand um die Rotation besser zu sehen...

				// jetzt noch die Rotation...

				Vector3 forward = dock.nodeTransform.forward;
				Vector3 right = Vector3.ProjectOnPlane(dock.nodeTransform.right, forward);
				Vector3 up = Vector3.ProjectOnPlane(dock.nodeTransform.up, forward);

				Vector3[] vs = { forward, right, up };

				Quaternion a, b = Quaternion.identity; float g;

				for(int i = 0; i < 3; i++)
				{
					a = Quaternion.Inverse(dock.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(vs[i], vs[(i + 1) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(dock.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(vs[i], vs[(i + 2) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(dock.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(-vs[i], vs[(i + 1) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(dock.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(-vs[i], vs[(i + 2) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(dock.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(vs[i], -vs[(i + 1) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(dock.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(vs[i], -vs[(i + 2) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(dock.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(-vs[i], -vs[(i + 1) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;

					a = Quaternion.Inverse(dock.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(-vs[i], -vs[(i + 2) % 3]);
					g = Quaternion.Angle(a, Quaternion.identity);
					if(g < 4)
						g = g;
				}

/*
				dock.sameVesselDockJoint.Joint.targetRotation =
					Quaternion.Inverse(dock.sameVesselDockJoint.Joint.transform.rotation) *
					Quaternion.LookRotation(
						Vector3.ProjectOnPlane(dock.nodeTransform.right, -dock.otherNode.nodeTransform.forward).normalized),
						Vector3.ProjectOnPlane(dock.nodeTransform.up, -dock.otherNode.nodeTransform.forward).normalized);
*/
				forward = -dock.otherNode.nodeTransform.forward;

				forward = Quaternion.FromToRotation(forward, dock.nodeTransform.forward) * dock.nodeTransform.forward;

				right = Vector3.ProjectOnPlane(dock.nodeTransform.right, forward);
				up = Vector3.ProjectOnPlane(dock.nodeTransform.up, forward);

		//		Vector3[] vs = { forward, right, up };

				dock.sameVesselDockJoint.Joint.targetRotation =
					Quaternion.Inverse(dock.sameVesselDockJoint.Joint.transform.rotation) *
						Quaternion.LookRotation(-up, forward);

			}
			else
			{
				// nur um zu sehen wohin das gehen soll
				dock.sameVesselDockJoint.Joint.connectedAnchor = 
					dock.otherNode.part.transform.InverseTransformPoint(
						dock.otherNode.nodeTransform.position);
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



//			Events["ContextMenuDisable"].active = (dock.fsm.CurrentState == dock.st_ready) || (dock.fsm.CurrentState == dock.st_disengage);
//			Events["ContextMenuDisable"].guiActive = (dock.fsm.CurrentState == dock.st_ready) || (dock.fsm.CurrentState == dock.st_disengage);

//			Events["ContextMenuEnable"].active = dock.fsm.CurrentState == dock.st_disabled;
//			Events["ContextMenuEnable"].guiActive = dock.fsm.CurrentState == dock.st_disabled;

//			Events["ContextMenuDecoupleAndDock"].active = dock.fsm.CurrentState == dock.st_preattached;
//			Events["ContextMenuDecoupleAndDock"].guiActive = dock.fsm.CurrentState == dock.st_preattached;

			DockStatus = dock.fsm.currentStateName;

			Events["TogglePort"].guiName = (dock.fsm.CurrentState == dock.st_disabled) ? "Activate Port" : "Deactivate Port";


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

		[KSPField(guiName = "dock status", isPersistant = false, guiActive = true)]
		public string DockStatus;

		[KSPField(guiName = "dock distance", isPersistant = false, guiActive = true)]
		public string DockDistance;

		[KSPField(guiName = "dock angle", isPersistant = false, guiActive = true)]
		public string DockAngle;

		[KSPEvent(guiActive = false, guiActiveUnfocused = false, guiName = "Deactivate Port", active = true)]
		public void TogglePort()
		{
			if(dock.fsm.CurrentState == dock.st_disabled)
				dock.fsm.RunEvent(on_activate);
			else
				dock.fsm.RunEvent(on_deactivate);

			UpdateUI();
		}

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = true, unfocusedRange = 2f, guiName = "#autoLOC_6001445")]
		public void Undock()
		{
// FEHLER hier das evtl. nicht tun, wenn's ein zweit-teil noch gibt -> dann umhängen

			dock.Undock();
		}

		////////////////////////////////////////
		// Actions

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
			alColor[4] = Color.blue;		// secondaryAxis
			alColor[5] = Color.white;
			alColor[6] = new Color(33.0f / 255.0f, 154.0f / 255.0f, 193.0f / 255.0f);
			alColor[7] = new Color(154.0f / 255.0f, 193.0f / 255.0f, 33.0f / 255.0f);
			alColor[8] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 154.0f / 255.0f);
			alColor[9] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 255.0f / 255.0f);
			alColor[10] = new Color(244.0f / 255.0f, 238.0f / 255.0f, 66.0f / 255.0f);
	//		alColor[11] = new Color(209.0f / 255.0f, 247.0f / 255.0f, 74.0f / 255.0f);
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
