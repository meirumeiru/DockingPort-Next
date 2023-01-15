using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KAS
{
	public class DockingPortEx2 : KASModuleHarpoon
	{
		private ModuleDockingNode dock = null;

		KFSMEvent on_enable2 = null, on_disable2 = null;

		public void Start()
		{
			try
			{
				dock = part.FindModuleImplementing<ModuleDockingNode>();
			}
			catch(Exception)
			{}
		}

		[KSPField(guiName = "dock status", isPersistant = false, guiActive = true)]
		public string DockStatus;

		public void Update()
		{
			try
			{
				Events["ContextMenuDisable"].active = (dock.fsm.CurrentState == dock.st_ready) || (dock.fsm.CurrentState == dock.st_disengage);
				Events["ContextMenuDisable"].guiActive = (dock.fsm.CurrentState == dock.st_ready) || (dock.fsm.CurrentState == dock.st_disengage);

				Events["ContextMenuEnable"].active = dock.fsm.CurrentState == dock.st_disabled;
				Events["ContextMenuEnable"].guiActive = dock.fsm.CurrentState == dock.st_disabled;

				Events["ContextMenuDecoupleAndDock"].active = dock.fsm.CurrentState == dock.st_preattached;
				Events["ContextMenuDecoupleAndDock"].guiActive = dock.fsm.CurrentState == dock.st_preattached;
			}
			catch(Exception)
			{}

			DockStatus = dock.fsm.currentStateName;
		}

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();
			Update();
		}

		public override void OnUpdate()
		{
			base.OnUpdate();
			Update();
		}

		[KSPEvent(name = "ContextMenuDisable", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "deactivate")]
		public void ContextMenuDisable()
		{
			if(on_disable2 == null)
			{
				on_disable2 = new KFSMEvent("Deactivate");
				on_disable2.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
				on_disable2.GoToStateOnEvent = dock.st_disabled;
				dock.fsm.AddEvent(on_disable2, new KFSMState[] { dock.st_ready, dock.st_disengage });
			}

			dock.fsm.RunEvent(on_disable2);
		}

		[KSPEvent(name = "ContextMenuEnable", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "activate")]
		public void ContextMenuEnable()
		{
			if(on_enable2 == null)
			{
				on_enable2 = new KFSMEvent("Activate");
				on_enable2.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
				on_enable2.GoToStateOnEvent = dock.st_ready;
				dock.fsm.AddEvent(on_enable2, new KFSMState[] { dock.st_disabled });
			}

			dock.fsm.RunEvent(on_enable2);
		}

		[KSPEvent(name = "ContextMenuDecoupleAndDock", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "decouple and dock")]
		public void ContextMenuDecoupleAndDock()
		{
			// das hier macht wohl ein... decouple and dock... obwohl ich's rausspringen lasse und es kommt
			// gleich zurück... aber, das geht offenbar...

			// mal sehen, ob ich die "main-second" Sache drehen kann... das wär echt super...
			// das docking auf Zuruf wird sicher noch interessant... aber, das kommt schon noch...

			// das ist das normale Decouple vom ModuleDockingPort... mal sehen ob's geht

			Part attachedPart = dock.referenceNode.attachedPart;
			if (dock.referenceNode.attachedPart == dock.part.parent)
			{
				dock.part.decouple(0f);
			}
			else
			{
				dock.referenceNode.attachedPart.decouple(0f);
			}

			// geht, aber ich brauch keine Force
		//	dock.part.AddForce(dock.nodeTransform.forward * (-dock.undockEjectionForce * 0.5f));
		//	attachedPart.AddForce(dock.nodeTransform.forward * (dock.undockEjectionForce * 0.5f));

			KFSMCallback e0 = dock.on_undock.OnEvent;
			KFSMCallback e1 = null;

			// wir schreiben die Funktionen um... -> damit müssten die Ports sofort wieder ready sein...
				dock.on_undock.OnEvent = delegate
				{
					// if's lass ich weg, ist mir egal
					dock.on_undock.GoToStateOnEvent = dock.st_ready; // direkt wieder ready
				};

				if(dock.otherNode)
				{
					e1 = dock.otherNode.on_undock.OnEvent;

					// das da oben ist Standard, ich definier das jetzt mal um....
					dock.otherNode.on_undock.OnEvent = delegate
					{
						// if's lass ich weg, ist mir egal
						dock.otherNode.on_undock.GoToStateOnEvent = dock.otherNode.st_ready; // direkt wieder ready
					};
				}

			dock.fsm.RunEvent(dock.on_undock);

			dock.on_undock.OnEvent = e0;

			if (dock.otherNode)
			{
				dock.otherNode.OnOtherNodeUndock();

				dock.otherNode.on_undock.OnEvent = e1;
			}
		}

		public static ModuleDockingNode dock3 = null;

		[KSPEvent(name = "ContextMenuDockSource", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "-- make new maindock")]
		public void ContextMenuDockSource()
		{
			dock3 = dock;
		}

		[KSPEvent(name = "ContextMenuRedock", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "-- undock")]
		public void ContextMenuRedock()
		{
			// undock... gut, das machen wir ganz normal eigentlich
			Part parent = dock.part.parent;
			Vessel vessel = dock.vessel;
			uint referenceTransformId = dock.vessel.referenceTransformId;
			if (parent != dock.otherNode.part)
			{
			//	dock.otherNode.Undock(); -> eigentlich alles verdreht machen... aber, davon gehe ich mal nicht aus, dass dies nötig sein wird jetzt gerade im Test
				return;
			}
			dock.part.Undock(dock.vesselInfo);
		//	dock.part.AddForce(dock.nodeTransform.forward * (-dock.undockEjectionForce * 0.5f));
		//	parent.AddForce(dock.nodeTransform.forward * (dock.undockEjectionForce * 0.5f));

			if (vessel == FlightGlobals.ActiveVessel && vessel[referenceTransformId] == null)
			{
				dock.StartCoroutine(dock.WaitAndSwitchFocus());
			}

			dock.fsm.RunEvent(dock.on_undock);
			dock.otherNode.OnOtherNodeUndock();

			// jetzt das undock same vessel machen aber gleich reaktivieren den Scheiss
				// dabei kommt's nicht drauf an, wer der other node und wer der eigentliche node ist... völlig wurscht

			// geht, aber ich brauch keine Force
			//	dock.part.AddForce(dock.nodeTransform.forward * (-dock.undockEjectionForce * 0.5f));
			//	attachedPart.AddForce(dock.nodeTransform.forward * (dock.undockEjectionForce * 0.5f));

			KFSMCallback e0 = dock3.on_undock.OnEvent;
			KFSMCallback e1 = null;

			// wir schreiben die Funktionen um... -> damit müssten die Ports sofort wieder ready sein...
			dock3.on_undock.OnEvent = delegate
			{
				// if's lass ich weg, ist mir egal
				dock3.on_undock.GoToStateOnEvent = dock3.st_ready; // direkt wieder ready
			};

			if(dock3.otherNode)
			{
				e1 = dock3.otherNode.on_undock.OnEvent;

				// das da oben ist Standard, ich definier das jetzt mal um....
				dock3.otherNode.on_undock.OnEvent = delegate
				{
					// if's lass ich weg, ist mir egal
					dock3.otherNode.on_undock.GoToStateOnEvent = dock3.otherNode.st_ready; // direkt wieder ready
				};
			}

			dock3.fsm.RunEvent(dock3.on_undock);

			dock3.on_undock.OnEvent = e0;

			if(dock3.otherNode)
			{
				dock3.otherNode.OnOtherNodeUndock();

				dock3.otherNode.on_undock.OnEvent = e1;
			}
		}
	}
}

