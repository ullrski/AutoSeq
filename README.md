# AutoSeq
Sequencer for Dynamics 365 Sales Auto Numbering

Sequencer Workflow Plugin for d365 CRM v9+ returns a string that is one unit greater than the string of characters 
in a sequentional numbering system. The sequentional numbering system is defined by a character string entry
in Attribute/Column "extentec_base" (Base List) in the Entity/Table "extentec_sequence" (Sequencer). A different 
Sequencer is configured for each desired sequence and tested to be 1:1 with an Attribute/Column on any OOB or custom
entity. Using 1 Sequencer for multiple Attribute/Columns or in multiple Entity/Tables was not tested.
 
d365 CRM Workflow will supply the selected Entity record. The output incremented string is intended to be 
used for Autonumbering or other operations and tested to work in conjunction with JLattimer's sting utilities
https://github.com/jlattimer/CRM-String-Workflow-Utilities

Dependencies:  Entity/Table "extentec_sequence" with required non-null extentec_base and extentec_seed
Extentec AutoSeq was created by Absolute Insight, Inc. for Extentec
Eric Stearns (ullrski @ github) https://github.com/ullrski
Copyright (c) 2021 
Released & licensed under under the MIT License
Created using XRMToolkit 7.3.0.0 (https://xrmtoolkit.com/)
 
#d365 #autonumber
     
