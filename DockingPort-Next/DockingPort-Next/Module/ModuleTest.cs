using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;


namespace DockingPort_Next.Module
{
	public class ModuleTest : PartModule
	{
		////////////////////////////////////////
		// Constructor

		public ModuleTest()
		{
		}

		////////////////////////////////////////
		// Callbacks

		public override void OnAwake()
		{
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);
		}

		public void OnDestroy()
		{
		}

		////////////////////////////////////////
		// Functions

		// FEHLER, temp because of problems with model
		private void getch(Transform t, List<Transform> tl, string nm)
		{
			for(int i = 0; i < t.childCount; i++)
			{
				Transform tc = t.GetChild(i);
	//			SkeletonBone b = tc.GetComponent<SkeletonBone>();
	//			if(nm == tc.name)
					tl.Add(tc);
	//			else
					getch(tc, tl, nm);
			}
		}

		[KSPField(guiName = "last transform", isPersistant = false, guiActive = true)]
		public string lastTransform;

int idx = 0;

		private void LookAtTop(List<Transform> tl, string idx)
		{
			string st = "pistonTop" + idx;
			string sb = "pistonBottom" + idx + ".tgt";

			Transform tb = null, tt = null;

			for(int i = 0; i < tl.Count; i++)
			{
				if(tl[i].name == st)
					tt = tl[i];
				if(tl[i].name == sb)
					tb = tl[i];
			}

			tt.LookAt(tb);
			tt.RotateAround(tt.position, tt.right, 90f);
		}

		private void LookAtBottom(List<Transform> tl, string idx)
		{
			string st = "pistonBottom" + idx;
			string sb = "pistonTop" + idx + ".tgt";

			Transform tb = null, tt = null;

			for(int i = 0; i < tl.Count; i++)
			{
				if(tl[i].name == st)
					tt = tl[i];
				if(tl[i].name == sb)
					tb = tl[i];
			}

			tt.LookAt(tb);
			Quaternion r1 = tt.rotation;
			tt.RotateAround(tt.position, tt.right, 90f);
			Quaternion r2 = Quaternion.Inverse(r1) * tt.rotation;
Quaternion r3 = Quaternion.LookRotation(Vector3.up);
Quaternion r4 = Quaternion.LookRotation(Vector3.right);
		}

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = true, unfocusedRange = 2f, guiName = "Test")]
		public void Test()
		{
			string s = "pistonTop1.L";
			s = "pistonTop1.L.tgt";

			s = "pistonBottom1.L";
			s = "pistonBottom1.L.tgt";

			// FEHLER, temp because of problems with model
			List<Transform> tl = new List<Transform>();
			getch(transform, tl, "dockingRing");

			int i = 0;
			while(tl[i].name != "dockingRing") ++i;

			tl[i].localPosition = Vector3.zero;
			tl[i].Translate(Vector3.up * 0.4f);


			tl = new List<Transform>();
			getch(transform, tl, s);

/*
			while(idx < tl.Count)
			{
				if(tl[idx].name.StartsWith("piston"))
				{
					tl[idx].LookAt(Vector3.up * 5f);
					lastTransform = tl[idx].name;
					++idx;
					return;
				}
				++idx;
			}

			lastTransform = "";
			idx = 0;
*/

			LookAtTop(tl, "1.L");
			LookAtTop(tl, "1.R");
			LookAtTop(tl, "2.L");
			LookAtTop(tl, "2.R");
			LookAtTop(tl, "3.L");
			LookAtTop(tl, "3.R");

			LookAtBottom(tl, "1.L");
			LookAtBottom(tl, "1.R");
			LookAtBottom(tl, "2.L");
			LookAtBottom(tl, "2.R");
			LookAtBottom(tl, "3.L");
			LookAtBottom(tl, "3.R");


/*

			SkinnedMeshRenderer[] mr = part.GetComponentsInChildren<SkinnedMeshRenderer>();

			for(int i = 0; i < mr[0].bones.Length; i++)
			{
//				if(mr[0].bones[i].name == "dockingRing")
//				mr[0].bones[i].transform.Translate(Vector3.up);

				mr[0].bones[i].LookAt(Vector3.up * 10f);
			}

*/
		}
	}
}
