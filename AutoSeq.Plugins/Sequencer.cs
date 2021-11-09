using System;

using System.Activities;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace AutoSeq.Plugins {
    public partial class Sequencer:BaseWorkflow {
        // Get The Table And Column Name
        [RequiredArgument]
        [Input("Select a Sequence")]
        [ReferenceTarget("extentec_sequence")]
        public InArgument<EntityReference> SelectedSequence { get; set; }

        [Output("Sequence String")]
        public OutArgument<string> SequenceString { get; set; }

        protected override void ExecuteInternal(LocalWorkflowContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            var selectedSequence = this.SelectedSequence.Get(context.CodeActivityContext);

            if (!selectedSequence.KeyAttributes.TryGetValue("extentec_seqcurrent", out var sequenceString)) {
                selectedSequence.KeyAttributes.Add("extentec_seqcurrent",
                    selectedSequence.KeyAttributes.TryGetValue("extentec_seed", out var seed).ToString());
                    sequenceString = seed;
            }

            try {
                SequenceString.Set(context.CodeActivityContext,sequenceString.ToString());

                if (!IncrementSequence(selectedSequence)) {
                    throw new NotImplementedException(nameof(context));
                }
            } catch (Exception) {
                throw;
            }
        }

        private bool IncrementSequence(EntityReference selectedSequence) {

            //Declare Vars
            string[] baseArray;

            //Move Seq Forward and get Sequence & Base List to increment & save to newNext
            if (selectedSequence.KeyAttributes.TryGetValue("extentec_base", out var baseList)) {

                //split list
                baseArray = baseList.ToString().Split(',');

                //move sequence and get array if sequence move success
                if (MoveSequence(selectedSequence)) {

                    //new current to increment and save to newNext
                    if (selectedSequence.KeyAttributes.TryGetValue("extentec_seqcurrent", out var newCurrent)) {

                        selectedSequence.KeyAttributes.Add("extentec_seqnext", Increment(newCurrent.ToString().Split(), baseArray));
                        return true;

                    } else { throw new ArgumentNullException(nameof(selectedSequence)); }

                    //If move = false then need to update current and add next
                } else if (selectedSequence.KeyAttributes.TryGetValue("extentec_seed", out var seed)) {

                    //update current
                    selectedSequence.KeyAttributes.Add("extentec_seqcurrent", Increment(seed.ToString().Split(), baseArray));

                    if (selectedSequence.KeyAttributes.TryGetValue("extentec_seqcurrent", out var newCurrent)) {

                        //add next
                        selectedSequence.KeyAttributes.Add("extentec_seqnext", Increment(newCurrent.ToString().Split(), baseArray));

                        return true;

                    } else { throw new ArgumentNullException(nameof(selectedSequence)); }
                } else { return false; }
            } else { throw new ArgumentNullException(nameof(selectedSequence)); }         //TODO: Default to {real integers} 
        }

        private bool MoveSequence(EntityReference selectedSequence) {

            //If oldCurrent not Null then Set newLast = oldCurrent
            if (selectedSequence.KeyAttributes.TryGetValue("extentec_seqcurrent", out var oldCurrentInSeq)) {

                //Set newLast = oldCurrent
                selectedSequence.KeyAttributes.Add("extentec_seqlast", oldCurrentInSeq.ToString());

                //If oldNext not Null then Set newCurrent = oldNext
                if (selectedSequence.KeyAttributes.TryGetValue("extentec_seqnext", out var oldNextInSeq)) {

                    //Set newCurrent = oldNext
                    selectedSequence.KeyAttributes.Add("extentec_seqcurrent", oldNextInSeq.ToString());

                    return true;  //return to set newNext

                } else {

                    return false; //return to set newNext and newCurrent

                }
            } else { throw new ArgumentNullException(nameof(selectedSequence)); }
        }

        private string Increment(string[] sequenceArray, string[] baseArray) {
            //Increment the sequence
            try {

                bool outOfRange;
                int range = sequenceArray.Length;

                do {

                    try {

                        //From Base List replace last char in (sequenceArray or Seed) with next char
                        int indexOfLastChar = Array.IndexOf(baseArray, sequenceArray[range - 1]);
                        int indexOfNextChar = indexOfLastChar + 1;

                        //set last in sequence to new from base, throw if OutofRange
                        sequenceArray[range - 1] = baseArray[indexOfNextChar];
                        outOfRange = false;

                    } catch (IndexOutOfRangeException) {

                        //That was the last char in the base, increment the next unit factor
                        outOfRange = true;
                        range--;
                    }
                } while (outOfRange && (range - 1 > 0));

                return sequenceArray.ToString();

            } catch (Exception) {                
                throw;
            }
        }
    }
}

