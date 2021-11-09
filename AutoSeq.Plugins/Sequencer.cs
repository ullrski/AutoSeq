using System;

using System.Activities;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace AutoSeq.Plugins {
    public partial class Sequencer : BaseWorkflow
    {
        // Get The Table And Column Name
        [RequiredArgument]
        [Input("Select a Sequence")]
        [ReferenceTarget("extentec_sequence")]
        public InArgument<EntityReference> SelectedSequence { get; set; }

        [Output("Sequence String")]
        public OutArgument<string> SequenceString { get; set; }

        protected override void ExecuteInternal(LocalWorkflowContext context) 
        {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            var selectedSequence = this.SelectedSequence.Get(context.CodeActivityContext);

            try {
                SequenceString.Set(
                    context.CodeActivityContext,
                    selectedSequence.KeyAttributes.TryGetValue(
                        "extentec_seqcurrent",
                        out var sequenceString).ToString()
                    );

                if (!IncrementSequence(selectedSequence)){
                    throw new NotImplementedException(nameof(context));
                }
            } catch (Exception) {
                throw;
            }
        }

        private bool IncrementSequence(EntityReference selectedSequence) {

            string sequence;
            string[] baseArray;
            string[] sequenceArray;

            //Get Sequence Base List
            if (selectedSequence.KeyAttributes.TryGetValue("extentec_base", out var baseList)) {

                //split list
                baseArray = baseList.ToString().Split(',');

            } else { throw new ArgumentNullException(nameof(selectedSequence)); }

            //Set new Current = old Next
            if (selectedSequence.KeyAttributes.TryGetValue("extentec_seqnext", out var oldNextInSeq)) {
                
                //get old for increment
                sequence = oldNextInSeq.ToString();

                //Save to extentec_seqcurrent
                selectedSequence.KeyAttributes.Add("extentec_seqcurrent", oldNextInSeq);

            } else { throw new ArgumentNullException(nameof(selectedSequence)); }

            //Increment the sequence
            //get sequence split
            try {

                sequenceArray = sequence.Split();

                bool outOfRange;
                int range = sequenceArray.Length;

                do {

                    try {

                        //From Base List replace last char in (sequenceArray or Seed) with next char
                        int indexOfLastChar = Array.IndexOf(baseArray, sequenceArray[range]);
                        int indexOfNextChar = indexOfLastChar + 1;
                        
                        //set last in sequence to new from base, throw if OutofRange
                        sequenceArray[range] = baseArray[indexOfNextChar];
                        outOfRange = false;

                    } catch (IndexOutOfRangeException) {

                        //That was the last char in the base, increment the next unit factor
                        outOfRange = true;
                        range--;
                    }
                } while (outOfRange && (range>=0));

                return true;

            } catch (Exception) {
                return false;
                throw; 
            }
        }
    }
}

