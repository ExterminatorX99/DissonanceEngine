﻿using System;
using System.Collections.Generic;

namespace Dissonance.Engine
{
	partial class Input
	{
		private static InputTrigger[] triggers;
		private static Dictionary<string,InputTrigger> triggersByName;

		public static InputTrigger RegisterTrigger(string name,InputBinding[] bindings,float? minValue = null,float? maxValue = null)
			=> RegisterTrigger(typeof(InputTrigger),name,bindings,minValue,maxValue);

		internal static InputTrigger RegisterTrigger(Type type,string name,InputBinding[] bindings,float? minValue = null,float? maxValue = null)
		{
			if(triggersByName.TryGetValue(name,out var trigger)) {
				trigger.Bindings = bindings;
				return trigger;
			}

			int id = triggers.Length;

			trigger = (InputTrigger)Activator.CreateInstance(type,true); //new InputTrigger();

			trigger.Init(id,name,bindings,minValue ?? InputTrigger.DefaultMinValue,maxValue ?? InputTrigger.DefaultMaxValue);

			Array.Resize(ref triggers,id+1);

			triggers[id] = trigger;
			triggersByName[name] = trigger;

			InputTrigger.Count = triggers.Length;

			return trigger;
		}

		private static void InitTriggers()
		{
			triggers = new InputTrigger[0];
			triggersByName = new Dictionary<string,InputTrigger>();
		}
		private static void UpdateTriggers()
		{
			for(int i = 0;i<triggers.Length;i++) {
				var trigger = triggers[i];
				ref var input = ref trigger.CurrentInput;

				input.prevAnalogInput = input.analogInput;
				input.wasPressed = input.isPressed;

				trigger.Value = 0f;

				for(int j = 0;j<trigger.bindingCount;j++) {
					trigger.Value += trigger.bindings[j].Value;
				}
			}
		}
		private static void TriggerSet(float value,string triggerName)
		{
			/*int hash = triggerName.GetHashCode();

			for(int i = 0;i<triggers.Length;i++) {
				var trigger = triggers[i];
				float fixedSumm = 0f;
				float renderSumm = 0f;

				

				trigger.SetAnalogValue(fixedSumm,renderSumm);
			}*/
		}
		private static void TriggerReset(string triggerName)
		{
			/*int hash = triggerName.GetHashCode();

			for(int i = 0;i<triggers.Length;i++) {
				var trigger = triggers[i];
				float fixedSumm = 0f;
				float renderSumm = 0f;

				for(int j = 0;j<trigger.bindingCount;j++) {
					ref var input = ref trigger.bindings[j];

					if(hash==input.InputHash && triggerName==input.InputLower) {
						input.fixedAnalogInput = 0f;
						input.renderAnalogInput = 0f;
					} else {
						if(input.fixedAnalogInput>input.deadZone || input.fixedAnalogInput<-input.deadZone) {
							fixedSumm += input.fixedAnalogInput;
						}

						if(input.renderAnalogInput>input.deadZone || input.renderAnalogInput<-input.deadZone) {
							renderSumm += input.renderAnalogInput;
						}
					}
				}

				trigger.SetAnalogValue(fixedSumm,renderSumm);
			}*/
		}
	}
}
