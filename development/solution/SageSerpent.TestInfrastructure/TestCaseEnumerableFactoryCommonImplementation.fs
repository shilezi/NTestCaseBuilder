﻿namespace SageSerpent.TestInfrastructure

    open System.Collections
    open System.Collections.Generic
    open System
    open SageSerpent.Infrastructure
    open SageSerpent.Infrastructure.RandomExtensions
    open SageSerpent.Infrastructure.IEnumerableExtensions

    type internal TestCaseEnumerableFactoryCommonImplementation (node: Node) =
        member this.Node = node
        
        interface ITestCaseEnumerableFactory with
            override this.CreateEnumerable maximumDesiredStrength =
                match node.PruneTree with
                    Some prunedNode ->
                        let associationFromStrengthToPartialTestVectorRepresentations
                            , associationFromTestVariableIndexToNumberOfItsLevels =
                            prunedNode.AssociationFromStrengthToPartialTestVectorRepresentations maximumDesiredStrength
                        let randomBehaviour =
                            Random 0
                        let randomBehaviourConsumerProducingSequenceOfFinalValues =
                            let mergedPartialTestVectorRepresentations =
                                MergedPartialTestVectorRepresentations.Initial
                                // Do a fold back so that high strength vectors get in there first. Hopefully the lesser strength vectors
                                // should have a greater chance of finding an earlier, larger vector to merge with this way. 
                                |> Map.foldBack (fun _
                                                     partialTestVectorsAtTheSameStrength
                                                     mergedPartialTestVectorRepresentations ->
                                                        partialTestVectorsAtTheSameStrength
                                                        |> Seq.fold (fun mergedPartialTestVectorRepresentations
                                                                         partialTestVector ->
                                                                            let mergedPartialTestVectorRepresentations =
                                                                                mergedPartialTestVectorRepresentations
                                                                            mergedPartialTestVectorRepresentations.MergeOrAdd partialTestVector
                                                                                                                              randomBehaviour)
                                                                    mergedPartialTestVectorRepresentations)
                                                 associationFromStrengthToPartialTestVectorRepresentations
                            seq
                                {
                                    for mergedPartialTestVector in mergedPartialTestVectorRepresentations do
                                        let filledOutPartialTestVectorRepresentation =
                                            prunedNode.FillOutPartialTestVectorRepresentation associationFromTestVariableIndexToNumberOfItsLevels
                                                                                              mergedPartialTestVector
                                                                                              randomBehaviour
                                        yield prunedNode.CreateFinalValueFrom filledOutPartialTestVectorRepresentation
                                }
                        randomBehaviourConsumerProducingSequenceOfFinalValues
                        :> IEnumerable
                        
                  | None ->
                        Seq.empty :> IEnumerable
            override this.MaximumStrength =
                match node.PruneTree with
                    Some prunedNode ->
                        prunedNode.MaximumStrengthOfTestVariableCombination
                  | None ->
                        0u
