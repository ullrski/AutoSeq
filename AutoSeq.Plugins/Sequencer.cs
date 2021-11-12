using System;

using System.Activities;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace AutoSeq.Plugins {
	public partial class Sequencer:BaseWorkflow {
		/*
		 * Sequencer Workflow Plugin for d365 CRM v9+ returns a string that is one unit greater than the string of characters 
		 * in a sequentional numbering system. The sequentional numbering system is defined by a character string entry
		 * in Attribute/Column "extentec_base" (Base List) in the Entity/Table "extentec_sequence" (Sequencer). A different 
		 * Sequencer is configured for each desired sequence and tested to be 1:1 with an Attribute/Column on any OOB or custom
		 * entity. Using 1 Sequencer for multiple Attribute/Columns or in multiple Entity/Tables was not tested.
		 *  
		 * d365 CRM Workflow will supply the selected Entity record. The output incremented string is intended to be 
		 * used for Autonumbering or other operations and tested to work in conjunction with JLattimer's sting utilities
		 * https://github.com/jlattimer/CRM-String-Workflow-Utilities
		 * 
		 * Dependencies:  Entity/Table "extentec_sequence" with required non-null extentec_base and extentec_seed
		 * Extentec AutoSeq was created by Absolute Insight, Inc. for Extentec
		 * Eric Stearns (ullrski @ github) https://github.com/ullrski
		 * Copyright (c) 2021 
		 * Released & licensed under under the MIT License
		 * Created using XRMToolkit 7.3.0.0 (https://xrmtoolkit.com/)
		 */

		// Get sequence reference
		[RequiredArgument]
		[Input("Select a Sequence")]
		[ReferenceTarget("extentec_sequence")]
		public InArgument<EntityReference> SelectedSequence { get; set; }

		// Return the new sequence
		[Output("Sequence String")]
		public OutArgument<string> SequenceString { get; set; }

		protected override void ExecuteInternal(LocalWorkflowContext context) {

			// Verify context exists
			if (context == null) {
				throw new ArgumentNullException(nameof(context));
			}

			// Get & verify the selected entity record
			Entity selectedSequencer = context.OrganizationService.Retrieve(
				 this.SelectedSequence.Get(context.CodeActivityContext).LogicalName,
				 this.SelectedSequence.Get(context.CodeActivityContext).Id,
				 new ColumnSet(
					 new String[] { "extentec_seqcurrent", "extentec_seed", "extentec_base", "extentec_seqlast", "extentec_seqnext" })
				 );
			
			if (selectedSequencer == null) {
				throw new ArgumentNullException(nameof(context));
			}

			// Get & verify the current sequence to use
			if (!selectedSequencer.TryGetAttributeValue("extentec_seqcurrent", out string currentSequence)) {
				//if null then use seed (required in the UI)
				currentSequence = selectedSequencer.Attributes["extentec_seed"].ToString();
			}

			// Get base array (required in UI)
			char[] baseArray = selectedSequencer.TryGetAttributeValue("extentec_base", out string baseString)
				? baseString.ToCharArray()
				: throw new InvalidWorkflowException("baseArray (Sequencer.Base) Is Null");

			// Get nextSequence
			if (!selectedSequencer.TryGetAttributeValue("extentec_seqnext", out string nextSequence)) {
				//new one if null (means this was the first so seed = current)
				nextSequence = Increment(currentSequence.ToCharArray(), baseArray);
			}
			
			/*
			 * Set the output = the current sequence
			 * Increment the sequence
			 * Move Sequencer forward one
			 * Update the Sequencer
			*/
			try {

				// Set the output =  currentSequence
				SequenceString.Set(context.CodeActivityContext, currentSequence);

				// Set newNext = the the next sequence
				string newNext = Increment(nextSequence.ToCharArray(), baseArray);

				// Move sequence forward for next use
				selectedSequencer.Attributes["extentec_seqnext"] = newNext;
				selectedSequencer.Attributes["extentec_seqcurrent"] = nextSequence;
				selectedSequencer.Attributes["extentec_seqlast"] = currentSequence;

				// update changes
				context.OrganizationService.Update(selectedSequencer);

			} catch (Exception ex) {
				throw new InvalidWorkflowException("AutoSeq.ExecuteInternal Exception: " + ex.Message);
			}
		}

		private string Increment(char[] sequence, char[] baselist) {
			
			bool outOfRange;
			int sequenceIndex = sequence.Length - 1;		

			// trap 0 length array
			if (sequenceIndex < 0) { 
				throw new InvalidWorkflowException("Length of sequence array {sequenceIndex} < 1, invalid for processing"); 
			}

			/*
			 * Increment the sequence by taking it as a string array and using a base array
			 * to change the sequence array element up by one in the base array list
			 * if the base arry list is out of bounds then go to the next left sequence
			 * array element repeat
			 */
			try {
				// increment, while range > 1 so no infinite loop
				do {
					//From Base List get the index of the current sequence character
					int baselistIndex = Array.IndexOf(baselist, sequence[sequenceIndex]);

					//Increment the baselistIndex to ID the next character that this sequenceIndex element will be
					baselistIndex++;

					/*
					* if baselistIndex > baselist.Length - 1, then the sequence[sequenceIndex] should be set to baselist[0]
					* and sequenceIndex decreased so the next loop can process the next place value
					* else use new character baselistIndex to return the new character and overwirte the current sequenceIndex character
					*/
					if (baselistIndex > (baselist.Length - 1)) {
						sequence[sequenceIndex] = baselist[0];	 //set char in current seq place value to first in base list
						sequenceIndex--;						 //move to the next to the lsft place value
						outOfRange = true;						 //go around again
					} else {
						sequence[sequenceIndex] = baselist[baselistIndex];  //set char in current seq place value to the next higher from base list 
						outOfRange = false;									//char was updated, exit loop
					}
				} while (outOfRange && !(sequenceIndex < 0));  //just in case, prevent infinite loop  with <0 limit

				//return the string sequence
				return string.Join("", sequence);

			} catch (Exception ex) {
				throw new InvalidWorkflowException("Increment Error: " + ex.Message);
			}
		}
	}
}

