﻿#light

#nowarn "40"

namespace SageSerpent.TestInfrastructure

    open System.Collections
    open System.Collections.Generic
    open System
    open SageSerpent.Infrastructure
    open Microsoft.FSharp.Collections
    
    type 'Result NodeVisitOperations = // A 'visitor' by any other name. :-)
        {
            TestVariableNodeResult: seq<Object> -> 'Result
            CombineResultsFromInterleavingNodeSubtrees: seq<'Result> -> 'Result
            CombineResultsFromSynthesizingNodeSubtrees: seq<'Result> -> 'Result
        }
    and Node =
        TestVariableNode of seq<Object>
      | InterleavingNode of seq<Node>
      | SynthesizingNode of seq<Node>
          
        static member private TraverseTree nodeOperations =
            let rec memoizedCalculation =
                BargainBasement.Memoize (fun node ->
                                            match node with
                                                TestVariableNode levels ->
                                                    nodeOperations.TestVariableNodeResult levels
                                              | InterleavingNode subtrees ->
                                                    subtrees
                                                    |> Seq.map (fun subtreeHead -> memoizedCalculation subtreeHead)
                                                    |> nodeOperations.CombineResultsFromInterleavingNodeSubtrees
                                              | SynthesizingNode subtrees ->
                                                    subtrees
                                                    |> Seq.map (fun subtreeHead -> memoizedCalculation subtreeHead)
                                                    |> nodeOperations.CombineResultsFromSynthesizingNodeSubtrees)
            memoizedCalculation
      
        static member private CountTestVariablesFor =
            Node.TraverseTree   {
                                    TestVariableNodeResult = fun _ -> 1u
                                    CombineResultsFromInterleavingNodeSubtrees = Seq.reduce (+)
                                    CombineResultsFromSynthesizingNodeSubtrees = Seq.reduce (+)
                                }
                                
        member this.CountTestVariables = Node.CountTestVariablesFor this
        
      
        static member private SumLevelCountsFromAllTestVariablesFor =
            Node.TraverseTree   {
                                    TestVariableNodeResult = fun levels -> uint32 (Seq.length levels)
                                    CombineResultsFromInterleavingNodeSubtrees = Seq.reduce (+)
                                    CombineResultsFromSynthesizingNodeSubtrees = Seq.reduce (+)
                                }
                                
        member this.SumLevelCountsFromAllTestVariables = Node.SumLevelCountsFromAllTestVariablesFor this
        
      
        static member private MaximumStrengthOfTestVariableCombinationFor = 
            Node.TraverseTree   {
                                    TestVariableNodeResult = fun _ -> 1u
                                    CombineResultsFromInterleavingNodeSubtrees = Seq.max
                                    CombineResultsFromSynthesizingNodeSubtrees = Seq.reduce (+)
                                }
                                
        member this.MaximumStrengthOfTestVariableCombination = Node.MaximumStrengthOfTestVariableCombinationFor this                                    
                                
        member this.AssociationFromTestVariableIndexToVariablesThatAreInterleavedWithIt =
            let rec walkTree node
                             indexForLeftmostTestVariable
                             interleavingTestVariableIndices
                             previousAssociationFromTestVariableIndexToVariablesThatAreInterleavedWithIt =
                match node with
                    TestVariableNode levels ->
                        let forwardInterleavingPairs =
                            interleavingTestVariableIndices
                            |> List.map (function interleavingTestVariableIndex ->
                                                    indexForLeftmostTestVariable
                                                    , interleavingTestVariableIndex)
                        let backwardInterleavingPairs =
                            interleavingTestVariableIndices
                            |> List.map (function interleavingTestVariableIndex ->
                                                    interleavingTestVariableIndex
                                                    , indexForLeftmostTestVariable)
                        indexForLeftmostTestVariable + 1u
                        , previousAssociationFromTestVariableIndexToVariablesThatAreInterleavedWithIt
                          |> List.append forwardInterleavingPairs
                          |> List.append backwardInterleavingPairs
                  | InterleavingNode subtreeRootNodes ->
                        let mergeAssociationFromSubtree (indexForLeftmostTestVariable
                                                         , interleavingTestVariableIndicesFromTheLeftSiblings
                                                         , previousAssociationFromTestVariableIndexToVariablesThatAreInterleavedWithIt)
                                                        subtreeRootNode =
                            let maximumTestVariableFromSubtree
                                , associationFromTestVariableIndexToVariablesThatAreInterleavedWithIt =
                                walkTree subtreeRootNode
                                         indexForLeftmostTestVariable
                                         interleavingTestVariableIndicesFromTheLeftSiblings
                                         previousAssociationFromTestVariableIndexToVariablesThatAreInterleavedWithIt
                            let testVariableIndicesFromNode =
                                List.init (int32 (maximumTestVariableFromSubtree - indexForLeftmostTestVariable))
                                          (fun variableCount -> uint32 variableCount + indexForLeftmostTestVariable)
                            maximumTestVariableFromSubtree
                            , List.append testVariableIndicesFromNode interleavingTestVariableIndicesFromTheLeftSiblings
                            , associationFromTestVariableIndexToVariablesThatAreInterleavedWithIt
                        let maximumTestVariableFromSubtree
                            , _
                            , associationFromTestVariableIndexToVariablesThatAreInterleavedWithIt =
                            subtreeRootNodes
                            |> Seq.fold mergeAssociationFromSubtree (indexForLeftmostTestVariable
                                                                     , interleavingTestVariableIndices
                                                                     , previousAssociationFromTestVariableIndexToVariablesThatAreInterleavedWithIt)
                        maximumTestVariableFromSubtree
                        , associationFromTestVariableIndexToVariablesThatAreInterleavedWithIt
                        
                  | SynthesizingNode subtreeRootNodes ->
                        let mergeAssociationFromSubtree (indexForLeftmostTestVariable
                                                         , previouslyMergedAssociationList)
                                                        subtreeRootNode =
                            walkTree subtreeRootNode indexForLeftmostTestVariable interleavingTestVariableIndices previouslyMergedAssociationList
                        subtreeRootNodes
                        |> Seq.fold mergeAssociationFromSubtree (indexForLeftmostTestVariable
                                                                 , previousAssociationFromTestVariableIndexToVariablesThatAreInterleavedWithIt)
            let _,
                result =
                    walkTree this 0u [] []
            HashMultiMap.Create result
                                
        member this.PartialTestVectorRepresentationsGroupedByStrengthUpToAndIncluding strength =
            if strength = 0u
            then raise (PreconditionViolationException "Strength must be non-zero.")
            else let rec walkTree node strength indexForLeftmostTestVariable =
                    match node with
                        TestVariableNode levels ->
                            [[[indexForLeftmostTestVariable]]]
                            , indexForLeftmostTestVariable + 1u
                            , [(indexForLeftmostTestVariable, levels
                                                              |> Seq.map Some)]
                            
                      | InterleavingNode subtreeRootNodes ->
                            let rec joinPairsAtEachStrength first second =
                                match first, second with
                                    _, [] ->
                                        first
                                  | [], _ ->
                                        second
                                  | headFromFirst :: tailFromFirst, headFromSecond :: tailFromSecond ->
                                        (List.append headFromFirst headFromSecond) :: (joinPairsAtEachStrength tailFromFirst tailFromSecond)
                            let mergeTestVariableCombinationsFromSubtree (previouslyMergedTestVariableCombinations
                                                                          , indexForLeftmostTestVariable
                                                                          , previousAssociationFromTestVariableIndexToItsLevels)
                                                                         subtreeRootNode =
                                let testVariableCombinationsGroupedByStrengthFromSubtree
                                    , maximumTestVariableIndexFromSubtree
                                    , associationFromTestVariableIndexToItsLevelsFromSubtree =
                                    walkTree subtreeRootNode strength indexForLeftmostTestVariable
                                let mergedTestVariableCombinationsGroupedByStrength =
                                    joinPairsAtEachStrength previouslyMergedTestVariableCombinations testVariableCombinationsGroupedByStrengthFromSubtree
                                let associationFromTestVariableIndexToItsLevels =
                                    List.append associationFromTestVariableIndexToItsLevelsFromSubtree previousAssociationFromTestVariableIndexToItsLevels
                                mergedTestVariableCombinationsGroupedByStrength
                                , maximumTestVariableIndexFromSubtree
                                , associationFromTestVariableIndexToItsLevels                               
                            subtreeRootNodes
                            |> Seq.fold mergeTestVariableCombinationsFromSubtree ([], indexForLeftmostTestVariable, [])
                        
                      | SynthesizingNode subtreeRootNodes ->
                            let gatherTestVariableCombinationsFromSubtree (previouslyGatheredTestVariableCombinations
                                                                           , indexForLeftmostTestVariable
                                                                           , previousAssociationFromTestVariableIndexToItsLevels)
                                                                          subtreeRootNode =
                                let testVariableCombinationsGroupedByStrengthFromSubtree
                                    , maximumTestVariableIndexFromSubtree
                                    , associationFromTestVariableIndexToItsLevelsFromSubtree =
                                    walkTree subtreeRootNode strength indexForLeftmostTestVariable
                                let gatheredTestVariableCombinationsGroupedBySubtreeAndThenByStrength =
                                    testVariableCombinationsGroupedByStrengthFromSubtree :: previouslyGatheredTestVariableCombinations
                                let associationFromTestVariableIndexToItsLevels =
                                    List.append associationFromTestVariableIndexToItsLevelsFromSubtree previousAssociationFromTestVariableIndexToItsLevels
                                gatheredTestVariableCombinationsGroupedBySubtreeAndThenByStrength
                                , maximumTestVariableIndexFromSubtree
                                , associationFromTestVariableIndexToItsLevels
                            // Using 'fold' causes 'resultsFromSubtrees' to be built up in reverse to the subtree sequence,
                            // and this reversal propagates consistently through the code below. The only way it could
                            // cause a problem would be due to the order of processing the subtrees, but because the combinations
                            // from sibling subtrees are simply placed in a list and because the test variable indices are already
                            // correctly calculated, it doesn't matter.
                            let testVariableCombinationsGroupedBySubtreeAndThenByStrength
                                , maximumTestVariableIndex
                                , associationFromTestVariableIndexToItsLevels =
                                subtreeRootNodes
                                |> Seq.fold gatherTestVariableCombinationsFromSubtree ([], indexForLeftmostTestVariable, [])
                            let maximumStrengthsFromSubtrees =
                                testVariableCombinationsGroupedBySubtreeAndThenByStrength
                                |> List.map (fun testVariableCombinationsForOneSubtreeGroupedByStrength ->
                                                uint32 (List.length testVariableCombinationsForOneSubtreeGroupedByStrength))
                            let testVariableCombinationsGroupedBySubtreeAndThenByStrength =
                                testVariableCombinationsGroupedBySubtreeAndThenByStrength
                                |> List.map List.to_array
                            // Remove the key for zero: we are not interested in zero strength combinations!
                            // Not even if we consider the case where a synthesizing node has no subtrees,
                            // because in that case there is nothing to apply a zero-total distribution to.
                            let distributionsOfStrengthsOverSubtreesAtEachTotalStrength =
                                Map.remove 0u (CombinatoricUtilities.ChooseContributionsToMeetTotalsUpToLimit maximumStrengthsFromSubtrees strength)
                            let addInTestVariableCombinationsForGivenStrength strength distributions partialResult =
                                let addInTestVariableCombinationsForAGivenDistribution partialResult distribution =
                                    let testVariableCombinationsBySubtree =
                                        (List.zip distribution testVariableCombinationsGroupedBySubtreeAndThenByStrength)
                                        |> List.map (function strength
                                                              , resultFromSubtree ->
                                                                    if strength > 0u
                                                                    then resultFromSubtree.[int32 (strength - 1u)]
                                                                    else [[]])
                                    let joinTestVariableCombinations first second =
                                        List.append first second
                                    let testVariableCombinationsBuiltFromCrossProduct =
                                        (BargainBasement.CrossProduct testVariableCombinationsBySubtree)
                                        |> List.map (List.reduce_left joinTestVariableCombinations)
                                    List.append testVariableCombinationsBuiltFromCrossProduct partialResult
                                (distributions |> Seq.fold addInTestVariableCombinationsForAGivenDistribution [])::partialResult
                            let testVariableCombinationsGroupedByStrength =
                                Map.fold_right addInTestVariableCombinationsForGivenStrength distributionsOfStrengthsOverSubtreesAtEachTotalStrength []
                            testVariableCombinationsGroupedByStrength
                            , maximumTestVariableIndex
                            , associationFromTestVariableIndexToItsLevels
                 let testVariableCombinationsGroupedByStrength
                     , _
                     , associationFromTestVariableIndexToItsLevels =
                     walkTree this strength 0u
                 let associationFromTestVariableIndexToItsLevels =
                    associationFromTestVariableIndexToItsLevels
                    |> Map.of_list 
                 let associationFromTestVariableIndexToVariablesThatAreInterleavedWithIt =
                    this.AssociationFromTestVariableIndexToVariablesThatAreInterleavedWithIt
                 let createTestVectorRepresentations testVariableCombination =
                    let sentinelEntriesForInterleavedTestVariableIndices =
                        testVariableCombination
                        |> List.map (fun testVariableIndex ->
                                        if associationFromTestVariableIndexToVariablesThatAreInterleavedWithIt.ContainsKey testVariableIndex
                                        then associationFromTestVariableIndexToVariablesThatAreInterleavedWithIt.FindAll testVariableIndex
                                        else [])
                        |> List.concat
                        |> Set.of_list
                        |> Set.to_list
                        |> List.map (fun testVariableIndex ->
                                        (testVariableIndex, None))
                    let testVariableCombinationSortedByDecreasingNumberOfLevels = // Using this sort order optimizes the cross product later on.
                        let numberOfLevelsForTestVariable testVariableIndex
                            = uint32 (Seq.length associationFromTestVariableIndexToItsLevels.[testVariableIndex])
                        testVariableCombination
                        |> List.sort (fun first second ->
                                        compare (numberOfLevelsForTestVariable second)
                                                (numberOfLevelsForTestVariable first))
                    let levelEntriesForTestVariableIndicesFromList =
                        testVariableCombinationSortedByDecreasingNumberOfLevels
                        |> List.map (fun testVariableIndex ->
                                     associationFromTestVariableIndexToItsLevels.[testVariableIndex]
                                     |> Seq.map (fun level -> testVariableIndex, level)
                                     |> List.of_seq)
                    levelEntriesForTestVariableIndicesFromList
                    |> BargainBasement.CrossProductWithCommonSuffix sentinelEntriesForInterleavedTestVariableIndices
                    |> List.map (fun testVectorRepresentationAsList ->
                                    Map.of_list testVectorRepresentationAsList)
                 testVariableCombinationsGroupedByStrength
                 |> List.map (fun testVariableCombinations ->
                                testVariableCombinations
                                |> Seq.map createTestVectorRepresentations
                                |> Seq.concat)
        
                            