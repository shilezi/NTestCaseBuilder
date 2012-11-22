﻿#nowarn "40"

namespace NTestCaseBuilder

    open System.Collections
    open System.Collections.Generic
    open System
    open SageSerpent.Infrastructure
    open SageSerpent.Infrastructure.OptionWorkflow
    open SageSerpent.Infrastructure.OptionExtensions
    open SageSerpent.Infrastructure.RandomExtensions
    open SageSerpent.Infrastructure.ContinuationWorkflow
    open Microsoft.FSharp.Collections

    module MergedPartialTestVectorRepresentationsDetail =
        type AugmentedInternalNode<'Level when 'Level: comparison> (internalNode: InternalNode<'Level>) =
            let numberOfLevelsForLeadingTestVariable =
                match internalNode with
                    {
                        SubtreeWithLesserLevelsForSameTestVariableIndex = subtreeWithLesserLevelsForSameTestVariableIndex
                        SubtreeWithGreaterLevelsForSameTestVariableIndex = subtreeWithGreaterLevelsForSameTestVariableIndex
                        SubtreeForFollowingIndices = BinaryTreeOfLevelsForTestVariable UnsuccessfulSearchTerminationNode
                    } ->
                        subtreeWithLesserLevelsForSameTestVariableIndex.NumberOfLevelsForLeadingTestVariable
                        + subtreeWithGreaterLevelsForSameTestVariableIndex.NumberOfLevelsForLeadingTestVariable
                  | {
                        SubtreeWithLesserLevelsForSameTestVariableIndex = subtreeWithLesserLevelsForSameTestVariableIndex
                        SubtreeWithGreaterLevelsForSameTestVariableIndex = subtreeWithGreaterLevelsForSameTestVariableIndex
                    } ->
                        1u
                        + subtreeWithLesserLevelsForSameTestVariableIndex.NumberOfLevelsForLeadingTestVariable
                        + subtreeWithGreaterLevelsForSameTestVariableIndex.NumberOfLevelsForLeadingTestVariable

            let multiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero =
                match internalNode with
                    {
                        SubtreeWithLesserLevelsForSameTestVariableIndex = subtreeWithLesserLevelsForSameTestVariableIndex
                        SubtreeWithGreaterLevelsForSameTestVariableIndex = subtreeWithGreaterLevelsForSameTestVariableIndex
                        SubtreeForFollowingIndices = subtreeForFollowingIndices
                    } ->
                        let candidates =
                            [ subtreeWithLesserLevelsForSameTestVariableIndex.MultiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero;
                              subtreeWithGreaterLevelsForSameTestVariableIndex.MultiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero;
                              optionWorkflow
                                {
                                    let! multiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero =
                                        subtreeForFollowingIndices.MultiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero
                                    return match multiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero with
                                            multiplicity
                                            , length -> multiplicity
                                                        , 1u + length
                                }]
                            |> Seq.filter Option.isSome
                            |> Seq.map Option.get
                        if Seq.isEmpty candidates
                        then
                            None
                        else
                            let longestLength =
                                candidates
                                |> Seq.map snd
                                |> Seq.max
                            let multiplicitySummedOverTheCandidatesSharingTheLongestLength =
                                candidates
                                |> Seq.filter (snd >> (fun length -> longestLength = length))
                                |> Seq.map fst
                                |> Seq.reduce (+)
                            Some (multiplicitySummedOverTheCandidatesSharingTheLongestLength, longestLength)

            let allSuccessfulPathsCorrespondToSufficesOfFullTestVectors =
                match internalNode with
                    {
                        SubtreeWithLesserLevelsForSameTestVariableIndex = subtreeWithLesserLevelsForSameTestVariableIndex
                        SubtreeWithGreaterLevelsForSameTestVariableIndex = subtreeWithGreaterLevelsForSameTestVariableIndex
                        SubtreeForFollowingIndices = subtreeForFollowingIndices
                    } ->
                        subtreeForFollowingIndices.AllSuccessfulPathsCorrespondToSufficesOfFullTestVectors
                        && subtreeWithLesserLevelsForSameTestVariableIndex.AllSuccessfulPathsCorrespondToSufficesOfFullTestVectors
                        && subtreeWithGreaterLevelsForSameTestVariableIndex.AllSuccessfulPathsCorrespondToSufficesOfFullTestVectors

            member this.InternalNode =
                internalNode

            member this.NumberOfLevelsForLeadingTestVariable =
                numberOfLevelsForLeadingTestVariable

            member this.MultiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero =
                multiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero

            member this.AllSuccessfulPathsCorrespondToSufficesOfFullTestVectors =
                allSuccessfulPathsCorrespondToSufficesOfFullTestVectors

        and InternalNode<'Level when 'Level: comparison> =
            {
                LevelForTestVariableIndex: 'Level
                SubtreeWithLesserLevelsForSameTestVariableIndex: BinaryTreeOfLevelsForTestVariable<'Level>
                SubtreeWithGreaterLevelsForSameTestVariableIndex: BinaryTreeOfLevelsForTestVariable<'Level>
                SubtreeForFollowingIndices: TernarySearchTree<'Level>
            }
        and BinaryTreeOfLevelsForTestVariable<'Level when 'Level: comparison> =
            UnsuccessfulSearchTerminationNode
          | AugmentedInternalNode of AugmentedInternalNode<'Level>

            member this.NumberOfLevelsForLeadingTestVariable =
                match this with
                    AugmentedInternalNode augmentedInternalNode ->
                        augmentedInternalNode.NumberOfLevelsForLeadingTestVariable
                  | UnsuccessfulSearchTerminationNode ->
                        0u

            member this.AllSuccessfulPathsCorrespondToSufficesOfFullTestVectors =
                match this with
                    AugmentedInternalNode augmentedInternalNode ->
                        augmentedInternalNode.AllSuccessfulPathsCorrespondToSufficesOfFullTestVectors
                  | UnsuccessfulSearchTerminationNode ->
                        true    // Yes, 'true' - because there are no successful paths. Remember that the result will
                                // be combined with those of sibling subtrees in a parent node, so we don't want an
                                // unnecessary 'false' in the combination if all the other siblings report 'true'.

            member this.MultiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero =
                match this with
                    AugmentedInternalNode augmentedInternalNode ->
                        augmentedInternalNode.MultiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero
                  | _ ->
                        None

        and WildcardNode<'Level when 'Level: comparison> =
            {
                SubtreeWithAllLevelsForSameTestVariableIndex: BinaryTreeOfLevelsForTestVariable<'Level>
                SubtreeForFollowingIndices: TernarySearchTree<'Level>
            }
        and TernarySearchTree<'Level when 'Level: comparison> =
            SuccessfulSearchTerminationNode
          | WildcardNode of WildcardNode<'Level>
          | BinaryTreeOfLevelsForTestVariable of BinaryTreeOfLevelsForTestVariable<'Level>

            member this.AllSuccessfulPathsCorrespondToSufficesOfFullTestVectors =
                match this with
                    SuccessfulSearchTerminationNode ->
                        true
                  | WildcardNode _ ->
                        false
                  | BinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable ->
                        binaryTreeOfLevelsForTestVariable.AllSuccessfulPathsCorrespondToSufficesOfFullTestVectors

            member this.MultiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero =
                match this with
                    SuccessfulSearchTerminationNode -> Some (1u, 0u)
                  | WildcardNode
                    {
                        SubtreeWithAllLevelsForSameTestVariableIndex = subtreeWithAllLevelsForSameTestVariableIndex
                    } -> subtreeWithAllLevelsForSameTestVariableIndex.MultiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero
                  | BinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable ->
                        binaryTreeOfLevelsForTestVariable.MultiplicityAndLengthOfLongestContiguousRunOfTestVariableIndicesFromZero

        type TreeSearchContextParameters =
            {
                TestVariableIndex: UInt32
                HasSuffixContextOfPossibleFullTestVector: Boolean
            }

            static member StartOfSearch =
                {
                    TestVariableIndex = 0u
                    HasSuffixContextOfPossibleFullTestVector = true
                }

            member this.IsSpecialCaseDenotingInitialState =   // The tests define this special state as not possessing any test vectors, not even the trivial empty one.
                0u = this.TestVariableIndex

            member this.IsFullTestVector maximumNumberOfTestVariables =
                this.HasSuffixContextOfPossibleFullTestVector
                && maximumNumberOfTestVariables = this.TestVariableIndex

            member this.PropagateFromDefinedLevelToNextTestVariable =
                {
                    this with
                        TestVariableIndex = this.TestVariableIndex + 1u
                }

            member this.PropagateFromWildcardLevelToNextTestVariable =
                {
                    this with
                        TestVariableIndex = this.TestVariableIndex + 1u
                        HasSuffixContextOfPossibleFullTestVector = false
                }

        let inline (|InternalNode|) (augmentedInternalNode: AugmentedInternalNode<'Level>) =
            augmentedInternalNode.InternalNode

        let inline InternalNode (internalNode: InternalNode<'Level>) =
            new AugmentedInternalNode<'Level> (internalNode)

        let inline mirrorInternalNode mirroring
                                      internalNodeRepresentation =
            if mirroring
            then
                match internalNodeRepresentation with
                    {
                        SubtreeWithLesserLevelsForSameTestVariableIndex = rootSubtreeWithLesserLevelsForSameTestVariableIndex
                        SubtreeWithGreaterLevelsForSameTestVariableIndex = rootSubtreeWithGreaterLevelsForSameTestVariableIndex
                    } ->
                        {
                            internalNodeRepresentation with
                                SubtreeWithLesserLevelsForSameTestVariableIndex = rootSubtreeWithGreaterLevelsForSameTestVariableIndex
                                SubtreeWithGreaterLevelsForSameTestVariableIndex = rootSubtreeWithLesserLevelsForSameTestVariableIndex
                        }
            else
                internalNodeRepresentation

        let inline (|MirroredInternalNode|) mirroring =
            (|InternalNode|)
            >> mirrorInternalNode mirroring

        let inline MirroredInternalNode mirroring =
            mirrorInternalNode mirroring
            >> InternalNode

        let splayInternalNodeWithMatchingOrNeighbouringLevel internalNodeRepresentation
                                                             comparisonWrtImplicitLevel =
            let mirroredComparisonWrtImplicitLevel =
                comparisonWrtImplicitLevel
                >> (~-)
            let rec accumulateFlankingSubtrees ({
                                                    LevelForTestVariableIndex = rootLevelForTestVariableIndex
                                                    SubtreeWithLesserLevelsForSameTestVariableIndex = rootSubtreeWithLesserLevelsForSameTestVariableIndex
                                                    SubtreeWithGreaterLevelsForSameTestVariableIndex = rootSubtreeWithGreaterLevelsForSameTestVariableIndex
                                                    SubtreeForFollowingIndices = rootSubtreeForFollowingIndices
                                                } as internalNodeRepresentationForRoot)
                                               addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels
                                               addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels
                                               interchangeRolesOfFlankingSubtreesOnBehalfOfCaller =
                let addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels
                    , addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels =
                    if interchangeRolesOfFlankingSubtreesOnBehalfOfCaller
                    then
                        addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels
                        , addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels
                    else
                        addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels
                        , addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels
                let inline splayAtRootNode () =
                    {
                        internalNodeRepresentationForRoot with
                            SubtreeWithLesserLevelsForSameTestVariableIndex = UnsuccessfulSearchTerminationNode
                            SubtreeWithGreaterLevelsForSameTestVariableIndex = UnsuccessfulSearchTerminationNode
                    }
                    , addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels rootSubtreeWithLesserLevelsForSameTestVariableIndex
                    , addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels rootSubtreeWithGreaterLevelsForSameTestVariableIndex
                match comparisonWrtImplicitLevel rootLevelForTestVariableIndex with
                    0 ->
                        // Degenerate root node only case (level has been found)...
                        splayAtRootNode ()
                  | rootResult ->
                        // NOTE: comments for this section refer to the 'unmirrored' case. So in the mirrored case,
                        // 'zig-zag' becomes 'zag-zig', 'least upper bound' becomes 'greatest lower bound', etc.
                        let localMirroring
                            , comparisonWrtImplicitLevel
                            , internalNodeRepresentationForRoot
                            , rootSubtreeWithLesserLevelsForSameTestVariableIndex
                            , rootSubtreeWithGreaterLevelsForSameTestVariableIndex
                            , addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels
                            , addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels =
                            if rootResult > 0
                            then
                                true
                                , mirroredComparisonWrtImplicitLevel
                                , mirrorInternalNode true
                                                     internalNodeRepresentationForRoot
                                , rootSubtreeWithGreaterLevelsForSameTestVariableIndex
                                , rootSubtreeWithLesserLevelsForSameTestVariableIndex
                                , addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels
                                , addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels
                            else
                                false
                                , comparisonWrtImplicitLevel
                                , internalNodeRepresentationForRoot
                                , rootSubtreeWithLesserLevelsForSameTestVariableIndex
                                , rootSubtreeWithGreaterLevelsForSameTestVariableIndex
                                , addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels
                                , addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels
                        match rootSubtreeWithLesserLevelsForSameTestVariableIndex with
                            AugmentedInternalNode
                            (MirroredInternalNode localMirroring
                            ({
                                LevelForTestVariableIndex = zigLevelForTestVariableIndex
                                SubtreeWithLesserLevelsForSameTestVariableIndex = zigSubtreeWithLesserLevelsForSameTestVariableIndex
                                SubtreeWithGreaterLevelsForSameTestVariableIndex = zigSubtreeWithGreaterLevelsForSameTestVariableIndex
                            } as internalNodeRepresentationForZig)) ->
                                match comparisonWrtImplicitLevel zigLevelForTestVariableIndex
                                      , zigSubtreeWithLesserLevelsForSameTestVariableIndex
                                      , zigSubtreeWithGreaterLevelsForSameTestVariableIndex with
                                    zigResult
                                    , AugmentedInternalNode (InternalNode internalNodeRepresentationforZigZig)
                                    , _ when zigResult < 0 ->
                                        // Zig-zig case...
                                        let addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels =
                                            (fun nodeWithLeastLevelToBeAddedToFlankingSubtreeWithGreaterLevels ->
                                                addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels
                                                    ({
                                                        internalNodeRepresentationForZig with
                                                            SubtreeWithLesserLevelsForSameTestVariableIndex = nodeWithLeastLevelToBeAddedToFlankingSubtreeWithGreaterLevels
                                                            SubtreeWithGreaterLevelsForSameTestVariableIndex =
                                                                {
                                                                    internalNodeRepresentationForRoot with
                                                                        SubtreeWithLesserLevelsForSameTestVariableIndex = zigSubtreeWithGreaterLevelsForSameTestVariableIndex
                                                                }
                                                                |> MirroredInternalNode localMirroring
                                                                |> AugmentedInternalNode
                                                    }
                                                    |> MirroredInternalNode localMirroring
                                                    |> AugmentedInternalNode))
                                        accumulateFlankingSubtrees
                                            internalNodeRepresentationforZigZig
                                            addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels
                                            addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels
                                            localMirroring
                                  | zigResult
                                    , _
                                    , AugmentedInternalNode (InternalNode internalNodeRepresentationforZigZag) when zigResult > 0 ->
                                        // Zig-zag case...
                                        let addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels =
                                            (fun nodeWithLeastLevelToBeAddedToFlankingSubtreeWithGreaterLevels ->
                                                addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels
                                                    ({
                                                        internalNodeRepresentationForRoot with
                                                            SubtreeWithLesserLevelsForSameTestVariableIndex = nodeWithLeastLevelToBeAddedToFlankingSubtreeWithGreaterLevels
                                                    }
                                                    |> MirroredInternalNode localMirroring
                                                    |> AugmentedInternalNode))
                                        let addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels =
                                            (fun nodeWithGreatestLevelToBeAddedToFlankingSubtreeWithLesserLevels ->
                                                addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels
                                                    ({
                                                        internalNodeRepresentationForZig with
                                                            SubtreeWithGreaterLevelsForSameTestVariableIndex = nodeWithGreatestLevelToBeAddedToFlankingSubtreeWithLesserLevels
                                                    }
                                                    |> MirroredInternalNode localMirroring
                                                    |> AugmentedInternalNode))
                                        accumulateFlankingSubtrees
                                            internalNodeRepresentationforZigZag
                                            addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels
                                            addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels
                                            localMirroring
                                  | _ ->
                                        // Zig-only case (either the level has been found, or found least upper bound instead)...
                                        let internalNodeRepresentationForSplayedZig =
                                            {
                                                internalNodeRepresentationForZig with
                                                    SubtreeWithLesserLevelsForSameTestVariableIndex = UnsuccessfulSearchTerminationNode
                                                    SubtreeWithGreaterLevelsForSameTestVariableIndex = UnsuccessfulSearchTerminationNode
                                            }
                                        let flankingSubtreeWithLesserLevels
                                            = addNodeWithGreatestLevelToFlankingSubtreeWithLesserLevels
                                                zigSubtreeWithLesserLevelsForSameTestVariableIndex
                                        let flankingSubtreeWithGreaterLevels
                                            = addNodeWithLeastLevelToFlankingSubtreeWithGreaterLevels
                                                ({
                                                    internalNodeRepresentationForRoot with
                                                        SubtreeWithLesserLevelsForSameTestVariableIndex = zigSubtreeWithGreaterLevelsForSameTestVariableIndex
                                                 }
                                                |> MirroredInternalNode localMirroring
                                                |> AugmentedInternalNode)
                                        if localMirroring
                                        then
                                            internalNodeRepresentationForSplayedZig
                                            , flankingSubtreeWithGreaterLevels
                                            , flankingSubtreeWithLesserLevels
                                        else
                                            internalNodeRepresentationForSplayedZig
                                            , flankingSubtreeWithLesserLevels
                                            , flankingSubtreeWithGreaterLevels
                          | _ ->
                                // Degenerate root node only case (level has not been found, found least upper bound instead)...
                                splayAtRootNode ()
            accumulateFlankingSubtrees internalNodeRepresentation
                                       BargainBasement.Identity
                                       BargainBasement.Identity
                                       false

    open MergedPartialTestVectorRepresentationsDetail

    type MergedPartialTestVectorRepresentations<'Level when 'Level: comparison>(ternarySearchTree: TernarySearchTree<'Level>,
                                                                                maximumNumberOfTestVariables: UInt32) =
        let createPartialTestVectorSequence revealFullTestVectorsAgain =
            let rec traverseTernarySearchTree ternarySearchTree
                                              (traversalContextParameters: TreeSearchContextParameters)
                                              partialTestVectorBeingBuilt =
                let rec traverseBinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable =
                    match binaryTreeOfLevelsForTestVariable with
                        UnsuccessfulSearchTerminationNode ->
                            if maximumNumberOfTestVariables <= traversalContextParameters.TestVariableIndex
                            then
                                raise (InternalAssertionViolationException "The test vector refers to test variable indices that are greater than the permitted maximum.")

                            Seq.empty
                      | AugmentedInternalNode
                        (InternalNode
                        {
                            LevelForTestVariableIndex = levelForTestVariableIndex
                            SubtreeWithLesserLevelsForSameTestVariableIndex = subtreeWithLesserLevelsForSameTestVariableIndex
                            SubtreeWithGreaterLevelsForSameTestVariableIndex = subtreeWithGreaterLevelsForSameTestVariableIndex
                            SubtreeForFollowingIndices = subtreeForFollowingIndices
                        }) ->
                            Seq.delay (fun() ->
                                        seq
                                            {
                                                yield! traverseBinaryTreeOfLevelsForTestVariable subtreeWithLesserLevelsForSameTestVariableIndex
                                                yield! traverseTernarySearchTree subtreeForFollowingIndices
                                                                                 traversalContextParameters.PropagateFromDefinedLevelToNextTestVariable
                                                                                 ((traversalContextParameters.TestVariableIndex, levelForTestVariableIndex) :: partialTestVectorBeingBuilt)
                                                yield! traverseBinaryTreeOfLevelsForTestVariable subtreeWithGreaterLevelsForSameTestVariableIndex
                                            })
                match ternarySearchTree with
                    SuccessfulSearchTerminationNode ->
                        if maximumNumberOfTestVariables < traversalContextParameters.TestVariableIndex
                            // NOTE: a subtlety - remember that 'testVariableIndex' can reach 'maximumNumberOfTestVariablesOverall'
                            // for a full (and possibly removed) test vector, because successful searches go through at least one
                            // node corresponding to each test variable index, *then* land on a node indicating whether the search
                            // was successful or not: so the zero-relative index gets incremented one more time.
                        then
                            raise (InternalAssertionViolationException "The test vector refers to test variable indices that are greater than the permitted maximum.")

                        if not revealFullTestVectorsAgain
                           && traversalContextParameters.IsFullTestVector maximumNumberOfTestVariables
                           || traversalContextParameters.IsSpecialCaseDenotingInitialState
                        then
                            Seq.empty
                        else
                            if (List.isEmpty partialTestVectorBeingBuilt)
                            then
                                raise (InternalAssertionViolationException "Should not contain an empty partial vector: attempts to merge in empty partial vectors should have resulted in the original collection.")

                            if uint32 partialTestVectorBeingBuilt.Length > maximumNumberOfTestVariables
                            then
                                raise (InternalAssertionViolationException "The test vector has more entries than the permitted maximum number of test variables.")

                            // NOTE: as we are converting to a map, we can be cavalier about the
                            // order in which associative pairs are added to the partial test vector.
                            Seq.singleton (partialTestVectorBeingBuilt
                                           |> Map.ofList)
                  | WildcardNode
                    {
                        SubtreeWithAllLevelsForSameTestVariableIndex = subtreeWithAllLevelsForSameTestVariableIndex
                        SubtreeForFollowingIndices = subtreeForFollowingIndices
                    } ->
                        Seq.delay (fun () ->
                                    seq
                                        {
                                            yield! traverseBinaryTreeOfLevelsForTestVariable subtreeWithAllLevelsForSameTestVariableIndex
                                            yield! traverseTernarySearchTree subtreeForFollowingIndices
                                                                             traversalContextParameters.PropagateFromWildcardLevelToNextTestVariable
                                                                             partialTestVectorBeingBuilt
                                        })
                  | BinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable ->
                        traverseBinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
            traverseTernarySearchTree ternarySearchTree
                                      TreeSearchContextParameters.StartOfSearch
                                      []

        let fillOutPartialTestVectorWithIndeterminates partialTestVectorRepresentation =
            if uint32 (partialTestVectorRepresentation: Map<_, _>).Count > maximumNumberOfTestVariables
            then
                raise (InternalAssertionViolationException "The partial test vector being either merged or added has more entries than the permitted maximum number of test variables.")

            let testVariableIndicesHavingLevels =
                partialTestVectorRepresentation
                |> Seq.map (fun keyValuePair -> keyValuePair.Key)
                |> Set.ofSeq

            let maximumTestVariableIndexHavingLevel =
                Seq.max testVariableIndicesHavingLevels

            if maximumTestVariableIndexHavingLevel >= maximumNumberOfTestVariables
            then
                raise (PreconditionViolationException "The partial test vector being either merged or added has a test variable index that is greater than the permitted maximum.")

            let testVariableIndicesForFilledOutTestVector = // NOTE: only up to 'maximumTestVariableIndexHavingLevel' exclusive
                                                            // - this is to avoid having a tail of indeterminate entries.
                Seq.init (int32 maximumTestVariableIndexHavingLevel) uint32
                |> Set.ofSeq

            let testVariableIndicesForIndeterminates =
                Set.difference testVariableIndicesForFilledOutTestVector testVariableIndicesHavingLevels

            let isPrefixOfFullTestVector =
                Set.isEmpty testVariableIndicesForIndeterminates

            if isPrefixOfFullTestVector
            then
                let isFullTestVector =
                    maximumNumberOfTestVariables = 1u + maximumTestVariableIndexHavingLevel
                partialTestVectorRepresentation
                |> Map.toList
                |> List.map (snd >> Some)
                , isFullTestVector
            else
                let sortedAssociationListFromTestVariableIndicesToIndeterminateMarkers =
                    testVariableIndicesForIndeterminates
                    |> Set.toList
                    |> List.map (fun testVariableIndex ->
                                     testVariableIndex
                                     , None)

                let sortedAssociationListFromTestVariableIndicesToLevels =
                    partialTestVectorRepresentation
                    |> Map.map (fun _ level ->
                                    Some level)
                    |> Map.toList

                let mergedAssociationList =
                    BargainBasement.MergeDisjointSortedAssociationLists sortedAssociationListFromTestVariableIndicesToLevels
                                                                        sortedAssociationListFromTestVariableIndicesToIndeterminateMarkers

                mergedAssociationList
                |> List.map snd
                , false


        let add ternarySearchTree
                newPartialTestVectorRepresentation =
            let rec addToTernarySearchTree ternarySearchTree
                                           newPartialTestVectorRepresentation =
                let buildDegenerateLinearSubtreeForDanglingSuffixOfNewPartialTestVectorRepresentation newPartialTestVectorRepresentation =
                        List.foldBack (fun optionalLevel
                                           degenerateLinearSubtree ->
                                            match optionalLevel with
                                                Some level ->
                                                    {
                                                        LevelForTestVariableIndex = level
                                                        SubtreeWithLesserLevelsForSameTestVariableIndex = UnsuccessfulSearchTerminationNode
                                                        SubtreeWithGreaterLevelsForSameTestVariableIndex = UnsuccessfulSearchTerminationNode
                                                        SubtreeForFollowingIndices = degenerateLinearSubtree
                                                    }
                                                    |> InternalNode
                                                    |> AugmentedInternalNode
                                                    |> BinaryTreeOfLevelsForTestVariable
                                              | None ->
                                                    {
                                                        SubtreeWithAllLevelsForSameTestVariableIndex = UnsuccessfulSearchTerminationNode
                                                        SubtreeForFollowingIndices = degenerateLinearSubtree
                                                    }
                                                    |> WildcardNode)
                                      newPartialTestVectorRepresentation SuccessfulSearchTerminationNode
                let rec addLevelToBinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
                                                                    levelFromNewPartialTestVectorRepresentation
                                                                    tailFromNewPartialTestVectorRepresentation =
                    match binaryTreeOfLevelsForTestVariable with
                        UnsuccessfulSearchTerminationNode ->
                            {
                                LevelForTestVariableIndex = levelFromNewPartialTestVectorRepresentation
                                SubtreeWithLesserLevelsForSameTestVariableIndex = UnsuccessfulSearchTerminationNode
                                SubtreeWithGreaterLevelsForSameTestVariableIndex = UnsuccessfulSearchTerminationNode
                                SubtreeForFollowingIndices =
                                    buildDegenerateLinearSubtreeForDanglingSuffixOfNewPartialTestVectorRepresentation tailFromNewPartialTestVectorRepresentation
                            }
                            |> InternalNode
                            |> AugmentedInternalNode
                      | AugmentedInternalNode
                        (InternalNode internalNodeRepresentation) ->
                            let comparisonWrtImplicitLevel =
                                compare levelFromNewPartialTestVectorRepresentation
                            let ({
                                    LevelForTestVariableIndex = splayedLevelForTestVariableIndex
                                    SubtreeForFollowingIndices = splayedSubtreeForFollowingIndices
                                 } as splayedInternalNodeRepresentation)
                                , flankingSubtreeWithLesserLevels
                                , flankingSubtreeWithGreaterLevels =
                                splayInternalNodeWithMatchingOrNeighbouringLevel internalNodeRepresentation
                                                                                 comparisonWrtImplicitLevel
                            match comparisonWrtImplicitLevel splayedLevelForTestVariableIndex with
                                result when result < 0 ->
                                    let flankingSubtreeWithGreaterLevels =
                                        {
                                            splayedInternalNodeRepresentation with
                                                SubtreeWithGreaterLevelsForSameTestVariableIndex = flankingSubtreeWithGreaterLevels
                                        }
                                        |> InternalNode
                                        |> AugmentedInternalNode
                                    {
                                        LevelForTestVariableIndex = levelFromNewPartialTestVectorRepresentation
                                        SubtreeWithLesserLevelsForSameTestVariableIndex = flankingSubtreeWithLesserLevels
                                        SubtreeWithGreaterLevelsForSameTestVariableIndex = flankingSubtreeWithGreaterLevels
                                        SubtreeForFollowingIndices =
                                            buildDegenerateLinearSubtreeForDanglingSuffixOfNewPartialTestVectorRepresentation tailFromNewPartialTestVectorRepresentation
                                    }
                                    |> InternalNode
                                    |> AugmentedInternalNode
                              | result when result > 0 ->
                                    let flankingSubtreeWithLesserLevels =
                                        {
                                            splayedInternalNodeRepresentation with
                                                SubtreeWithLesserLevelsForSameTestVariableIndex = flankingSubtreeWithLesserLevels
                                        }
                                        |> InternalNode
                                        |> AugmentedInternalNode
                                    {
                                        LevelForTestVariableIndex = levelFromNewPartialTestVectorRepresentation
                                        SubtreeWithLesserLevelsForSameTestVariableIndex = flankingSubtreeWithLesserLevels
                                        SubtreeWithGreaterLevelsForSameTestVariableIndex = flankingSubtreeWithGreaterLevels
                                        SubtreeForFollowingIndices =
                                            buildDegenerateLinearSubtreeForDanglingSuffixOfNewPartialTestVectorRepresentation tailFromNewPartialTestVectorRepresentation
                                    }
                                    |> InternalNode
                                    |> AugmentedInternalNode
                              | _ ->
                                    let modifiedSubtreeForFollowingIndices =
                                        addToTernarySearchTree splayedSubtreeForFollowingIndices
                                                               tailFromNewPartialTestVectorRepresentation
                                    {
                                        splayedInternalNodeRepresentation with
                                            SubtreeWithLesserLevelsForSameTestVariableIndex = flankingSubtreeWithLesserLevels
                                            SubtreeWithGreaterLevelsForSameTestVariableIndex = flankingSubtreeWithGreaterLevels
                                            SubtreeForFollowingIndices = modifiedSubtreeForFollowingIndices
                                    }
                                    |> InternalNode
                                    |> AugmentedInternalNode
                match ternarySearchTree
                      , newPartialTestVectorRepresentation with
                  | SuccessfulSearchTerminationNode
                    , _ :: _ ->
                        buildDegenerateLinearSubtreeForDanglingSuffixOfNewPartialTestVectorRepresentation newPartialTestVectorRepresentation
                  | WildcardNode
                    ({
                        SubtreeWithAllLevelsForSameTestVariableIndex = subtreeWithAllLevelsForSameTestVariableIndex
                     } as wildcardNodeRepresentation)
                    , Some levelFromNewPartialTestVectorRepresentation :: tailFromNewPartialTestVectorRepresentation ->
                        let modifiedSubtreeWithAllLevelsForSameTestVariableIndex =
                            addLevelToBinaryTreeOfLevelsForTestVariable subtreeWithAllLevelsForSameTestVariableIndex
                                                                        levelFromNewPartialTestVectorRepresentation
                                                                        tailFromNewPartialTestVectorRepresentation
                        {
                            wildcardNodeRepresentation with
                                SubtreeWithAllLevelsForSameTestVariableIndex = modifiedSubtreeWithAllLevelsForSameTestVariableIndex
                        } |> WildcardNode
                  | WildcardNode
                    ({
                        SubtreeForFollowingIndices = subtreeForFollowingIndices
                     } as wildcardNodeRepresentation)
                    , None :: tailFromNewPartialTestVectorRepresentation ->
                        let modifiedSubtreeForFollowingIndices =
                            addToTernarySearchTree subtreeForFollowingIndices
                                                   tailFromNewPartialTestVectorRepresentation
                        {
                            wildcardNodeRepresentation with
                                SubtreeForFollowingIndices = modifiedSubtreeForFollowingIndices
                        }
                        |> WildcardNode
                  | BinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
                    , Some levelFromNewPartialTestVectorRepresentation :: tailFromNewPartialTestVectorRepresentation ->
                        addLevelToBinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
                                                                    levelFromNewPartialTestVectorRepresentation
                                                                    tailFromNewPartialTestVectorRepresentation
                        |> BinaryTreeOfLevelsForTestVariable
                  | BinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
                    , None :: tailFromNewPartialTestVectorRepresentation ->
                        {
                            SubtreeWithAllLevelsForSameTestVariableIndex = binaryTreeOfLevelsForTestVariable
                            SubtreeForFollowingIndices = buildDegenerateLinearSubtreeForDanglingSuffixOfNewPartialTestVectorRepresentation tailFromNewPartialTestVectorRepresentation
                        }
                        |> WildcardNode
                  | _
                    , [] ->
                        raise (InternalAssertionViolationException "Attempt to add a new partial test vector representation that is already mergeable with or equivalent to a previous one.")
                        // The above is really a precondition violation, but the precondition should have been enforced at a higher level within the implementation and not by the client.
            addToTernarySearchTree ternarySearchTree
                                   newPartialTestVectorRepresentation

        let remove ternarySearchTree
                   queryPartialTestVectorRepresentation
                   existingFullTestVectorBlockedRemovalContinuation =
            let removeInternalNodeWithGreatestLevelInSubtree subtreeInternalNodeRepresentation =
                let comparisonWrtPositiveInfinity _ =
                    1
                let {
                        LevelForTestVariableIndex = splayedLevelForTestVariableIndex
                        SubtreeForFollowingIndices = splayedSubtreeForFollowingIndices
                    }
                    , flankingSubtreeWithLesserLevels
                    , _ =
                    splayInternalNodeWithMatchingOrNeighbouringLevel subtreeInternalNodeRepresentation
                                                                     comparisonWrtPositiveInfinity
                splayedLevelForTestVariableIndex
                , splayedSubtreeForFollowingIndices
                , flankingSubtreeWithLesserLevels
            let removeInternalNodeWithLeastLevelInSubtree subtreeInternalNodeRepresentation =
                let comparisonWrtNegativeInfinity _ =
                    -1
                let {
                        LevelForTestVariableIndex = splayedLevelForTestVariableIndex
                        SubtreeForFollowingIndices = splayedSubtreeForFollowingIndices
                    }
                    , _
                    , flankingSubtreeWithGreaterLevels =
                    splayInternalNodeWithMatchingOrNeighbouringLevel subtreeInternalNodeRepresentation
                                                                     comparisonWrtNegativeInfinity
                splayedLevelForTestVariableIndex
                , splayedSubtreeForFollowingIndices
                , flankingSubtreeWithGreaterLevels
            let buildResultSubtreeFromInternalNodeWithPruningOfDegenerateLinearSubtrees levelForTestVariableIndex
                                                                                        subtreeWithLesserLevelsForSameTestVariableIndex
                                                                                        subtreeWithGreaterLevelsForSameTestVariableIndex
                                                                                        subtreeForFollowingIndices =
                match subtreeWithLesserLevelsForSameTestVariableIndex
                      , subtreeWithGreaterLevelsForSameTestVariableIndex
                      , subtreeForFollowingIndices with
                  | UnsuccessfulSearchTerminationNode
                    , UnsuccessfulSearchTerminationNode
                    , BinaryTreeOfLevelsForTestVariable UnsuccessfulSearchTerminationNode ->
                        UnsuccessfulSearchTerminationNode
                  | UnsuccessfulSearchTerminationNode
                    , _
                    , BinaryTreeOfLevelsForTestVariable UnsuccessfulSearchTerminationNode ->
                        subtreeWithGreaterLevelsForSameTestVariableIndex
                  | _
                    , UnsuccessfulSearchTerminationNode
                    , BinaryTreeOfLevelsForTestVariable UnsuccessfulSearchTerminationNode ->
                        subtreeWithLesserLevelsForSameTestVariableIndex
                  | AugmentedInternalNode (InternalNode subtreeWithLesserLevelsForSameTestVariableIndexInternalNodeRepresentation)
                    , AugmentedInternalNode (InternalNode subtreeWithGreaterLevelsForSameTestVariableIndexInternalNodeRepresentation)
                    , BinaryTreeOfLevelsForTestVariable UnsuccessfulSearchTerminationNode ->
                        if subtreeWithLesserLevelsForSameTestVariableIndex.NumberOfLevelsForLeadingTestVariable
                           > subtreeWithGreaterLevelsForSameTestVariableIndex.NumberOfLevelsForLeadingTestVariable
                        then
                            let levelForTestVariableIndexFromRemovedNode
                                , subtreeForFollowingIndicesFromRemovedNode
                                , subtreeWithLesserLevelsForSameTestVariableIndexWithoutThatNode =
                                    removeInternalNodeWithGreatestLevelInSubtree subtreeWithLesserLevelsForSameTestVariableIndexInternalNodeRepresentation
                            ({
                                LevelForTestVariableIndex =
                                    levelForTestVariableIndexFromRemovedNode
                                SubtreeWithLesserLevelsForSameTestVariableIndex =
                                    subtreeWithLesserLevelsForSameTestVariableIndexWithoutThatNode
                                SubtreeWithGreaterLevelsForSameTestVariableIndex =
                                    subtreeWithGreaterLevelsForSameTestVariableIndex
                                SubtreeForFollowingIndices =
                                    subtreeForFollowingIndicesFromRemovedNode
                            }
                            |> InternalNode
                            |> AugmentedInternalNode)
                        else
                            let levelForTestVariableIndexFromRemovedNode
                                , subtreeForFollowingIndicesFromRemovedNode
                                , subtreeWithGreaterLevelsForSameTestVariableIndexWithoutThatNode =
                                    removeInternalNodeWithLeastLevelInSubtree subtreeWithGreaterLevelsForSameTestVariableIndexInternalNodeRepresentation
                            ({
                                LevelForTestVariableIndex =
                                    levelForTestVariableIndexFromRemovedNode
                                SubtreeWithLesserLevelsForSameTestVariableIndex =
                                    subtreeWithLesserLevelsForSameTestVariableIndex
                                SubtreeWithGreaterLevelsForSameTestVariableIndex =
                                    subtreeWithGreaterLevelsForSameTestVariableIndexWithoutThatNode
                                SubtreeForFollowingIndices =
                                    subtreeForFollowingIndicesFromRemovedNode
                            }
                            |> InternalNode
                            |> AugmentedInternalNode)
                  | _ ->
                        ({
                            LevelForTestVariableIndex =
                                levelForTestVariableIndex
                            SubtreeWithLesserLevelsForSameTestVariableIndex =
                                subtreeWithLesserLevelsForSameTestVariableIndex
                            SubtreeWithGreaterLevelsForSameTestVariableIndex =
                                subtreeWithGreaterLevelsForSameTestVariableIndex
                            SubtreeForFollowingIndices =
                                subtreeForFollowingIndices
                        }
                        |> InternalNode
                        |> AugmentedInternalNode)
            let buildResultSubtreeFromWildcardNodeWithPruningOfDegenerateLinearSubtrees subtreeWithAllLevelsForSameTestVariableIndex
                                                                                        subtreeForFollowingIndices =
                match subtreeWithAllLevelsForSameTestVariableIndex
                      , subtreeForFollowingIndices with
                    UnsuccessfulSearchTerminationNode
                    , BinaryTreeOfLevelsForTestVariable UnsuccessfulSearchTerminationNode ->
                        BinaryTreeOfLevelsForTestVariable UnsuccessfulSearchTerminationNode
                  | _
                    , BinaryTreeOfLevelsForTestVariable UnsuccessfulSearchTerminationNode ->
                        BinaryTreeOfLevelsForTestVariable subtreeWithAllLevelsForSameTestVariableIndex
                  | _ ->
                        {
                            SubtreeWithAllLevelsForSameTestVariableIndex =
                                subtreeWithAllLevelsForSameTestVariableIndex
                            SubtreeForFollowingIndices =
                                subtreeForFollowingIndices
                        }
                        |> WildcardNode
            let rec removeFromTernarySearchTree ternarySearchTree
                                                queryPartialTestVectorRepresentation
                                                (treeSearchContextParameters: TreeSearchContextParameters) =
                let buildResultFromInternalNodeModifyingSubtreeForFollowingTestVariableIndices tailFromQueryPartialTestVectorRepresentation
                                                                                               levelForTestVariableIndex
                                                                                               subtreeWithLesserLevelsForSameTestVariableIndex
                                                                                               subtreeWithGreaterLevelsForSameTestVariableIndex
                                                                                               subtreeForFollowingIndices =
                    continuationWorkflow
                        {
                            let! modifiedSubtreeForFollowingTestVariableIndices
                                 , removedPartialTestVector =
                                removeFromTernarySearchTree subtreeForFollowingIndices
                                                            tailFromQueryPartialTestVectorRepresentation
                                                            treeSearchContextParameters.PropagateFromDefinedLevelToNextTestVariable
                            let modifiedBinarySearchTree =
                                buildResultSubtreeFromInternalNodeWithPruningOfDegenerateLinearSubtrees levelForTestVariableIndex
                                                                                                        subtreeWithLesserLevelsForSameTestVariableIndex
                                                                                                        subtreeWithGreaterLevelsForSameTestVariableIndex
                                                                                                        modifiedSubtreeForFollowingTestVariableIndices
                            return modifiedBinarySearchTree
                                   , (Some levelForTestVariableIndex :: removedPartialTestVector)

                        }
                let buildResultFromWildcardNodeModifyingSubtreeForFollowingTestVariableIndices headFromQueryPartialTestVectorRepresentation
                                                                                               tailFromQueryPartialTestVectorRepresentation
                                                                                               subtreeWithAllLevelsForSameTestVariableIndex
                                                                                               subtreeForFollowingIndices =
                    continuationWorkflow
                        {
                            let! modifiedSubtreeForFollowingTestVariableIndices
                                 , removedPartialTestVector =
                                removeFromTernarySearchTree subtreeForFollowingIndices
                                                            tailFromQueryPartialTestVectorRepresentation
                                                            treeSearchContextParameters.PropagateFromWildcardLevelToNextTestVariable
                            let modifiedTernarySearchTree =
                                buildResultSubtreeFromWildcardNodeWithPruningOfDegenerateLinearSubtrees subtreeWithAllLevelsForSameTestVariableIndex
                                                                                                        modifiedSubtreeForFollowingTestVariableIndices
                            return modifiedTernarySearchTree
                                   , headFromQueryPartialTestVectorRepresentation :: removedPartialTestVector
                        }

                let rec removeLevelFromBinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
                                                                         levelFromQueryPartialTestVectorRepresentation
                                                                         tailFromQueryPartialTestVectorRepresentation =
                    match binaryTreeOfLevelsForTestVariable with
                        UnsuccessfulSearchTerminationNode ->
                            if maximumNumberOfTestVariables <= treeSearchContextParameters.TestVariableIndex
                            then
                                raise (InternalAssertionViolationException "The test vector refers to test variable indices that are greater than the permitted maximum.")

                            continuationWorkflow.Zero ()
                      | AugmentedInternalNode
                        (InternalNode internalNodeRepresentation) ->
                            let comparisonWrtImplicitLevel =
                                compare levelFromQueryPartialTestVectorRepresentation
                            let {
                                    LevelForTestVariableIndex = splayedLevelForTestVariableIndex
                                    SubtreeForFollowingIndices = splayedSubtreeForFollowingIndices
                                }
                                , flankingSubtreeWithLesserLevels
                                , flankingSubtreeWithGreaterLevels =
                                splayInternalNodeWithMatchingOrNeighbouringLevel internalNodeRepresentation
                                                                                 comparisonWrtImplicitLevel
                            match comparisonWrtImplicitLevel splayedLevelForTestVariableIndex with
                                0 ->
                                    buildResultFromInternalNodeModifyingSubtreeForFollowingTestVariableIndices tailFromQueryPartialTestVectorRepresentation
                                                                                                               splayedLevelForTestVariableIndex
                                                                                                               flankingSubtreeWithLesserLevels
                                                                                                               flankingSubtreeWithGreaterLevels
                                                                                                               splayedSubtreeForFollowingIndices
                              | _ ->
                                    continuationWorkflow.Zero ()
                and removeWildcardLevelFromBinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
                                                                             tailFromQueryPartialTestVectorRepresentation =
                    match binaryTreeOfLevelsForTestVariable with
                        UnsuccessfulSearchTerminationNode ->
                            if maximumNumberOfTestVariables <= treeSearchContextParameters.TestVariableIndex
                            then
                                raise (InternalAssertionViolationException "The test vector refers to test variable indices that are greater than the permitted maximum.")

                            continuationWorkflow.Zero ()

                      | AugmentedInternalNode
                        (InternalNode
                        {
                            LevelForTestVariableIndex = levelForTestVariableIndex
                            SubtreeWithLesserLevelsForSameTestVariableIndex = subtreeWithLesserLevelsForSameTestVariableIndex
                            SubtreeWithGreaterLevelsForSameTestVariableIndex = subtreeWithGreaterLevelsForSameTestVariableIndex
                            SubtreeForFollowingIndices = subtreeForFollowingIndices
                        }) ->
                            buildResultFromInternalNodeModifyingSubtreeForFollowingTestVariableIndices tailFromQueryPartialTestVectorRepresentation
                                                                                                       levelForTestVariableIndex
                                                                                                       subtreeWithLesserLevelsForSameTestVariableIndex
                                                                                                       subtreeWithGreaterLevelsForSameTestVariableIndex
                                                                                                       subtreeForFollowingIndices
                            + continuationWorkflow
                                {
                                    let! modifiedSubtreeWithLesserLevelsForSameTestVariableIndex
                                         , removedPartialTestVector =
                                        removeWildcardLevelFromBinaryTreeOfLevelsForTestVariable subtreeWithLesserLevelsForSameTestVariableIndex
                                                                                                 tailFromQueryPartialTestVectorRepresentation
                                    let modifiedBinaryTree =
                                        buildResultSubtreeFromInternalNodeWithPruningOfDegenerateLinearSubtrees levelForTestVariableIndex
                                                                                                                modifiedSubtreeWithLesserLevelsForSameTestVariableIndex
                                                                                                                subtreeWithGreaterLevelsForSameTestVariableIndex
                                                                                                                subtreeForFollowingIndices
                                    return modifiedBinaryTree
                                           , removedPartialTestVector
                                }
                            + continuationWorkflow
                                {
                                    let! modifiedSubtreeWithGreaterLevelsForSameTestVariableIndex
                                         , removedPartialTestVector =
                                        removeWildcardLevelFromBinaryTreeOfLevelsForTestVariable subtreeWithGreaterLevelsForSameTestVariableIndex
                                                                                                 tailFromQueryPartialTestVectorRepresentation
                                    let modifiedBinaryTree =
                                        buildResultSubtreeFromInternalNodeWithPruningOfDegenerateLinearSubtrees levelForTestVariableIndex
                                                                                                                subtreeWithLesserLevelsForSameTestVariableIndex
                                                                                                                modifiedSubtreeWithGreaterLevelsForSameTestVariableIndex
                                                                                                                subtreeForFollowingIndices
                                    return modifiedBinaryTree
                                           , removedPartialTestVector
                                }
                let adaptResultToRemovalFromTernaryTree resultFromRemovalFromBinaryTree =
                    continuationWorkflow
                        {
                            let! modifiedSubtreeWithAllLevelsForSameTestVariableIndex
                                 , removedPartialTestVector = resultFromRemovalFromBinaryTree
                            return BinaryTreeOfLevelsForTestVariable modifiedSubtreeWithAllLevelsForSameTestVariableIndex
                                   , removedPartialTestVector
                        }
                match ternarySearchTree
                      , queryPartialTestVectorRepresentation with
                    SuccessfulSearchTerminationNode
                    , _ ->
                        if maximumNumberOfTestVariables < treeSearchContextParameters.TestVariableIndex
                            // NOTE: a subtlety - remember that 'testVariableIndex' can reach 'maximumNumberOfTestVariablesOverall'
                            // for a full (and possibly removed) test vector, because successful searches go through at least one
                            // node corresponding to each test variable index, *then* land on a node indicating whether the search
                            // was successful or not: so the zero-relative index gets incremented one more time.
                        then
                            raise (InternalAssertionViolationException "The test vector refers to test variable indices that are greater than the permitted maximum.")

                        continuationWorkflow
                            {
                                if treeSearchContextParameters.IsFullTestVector maximumNumberOfTestVariables
                                then
                                    return! existingFullTestVectorBlockedRemovalContinuation ()
                                else
                                    return BinaryTreeOfLevelsForTestVariable UnsuccessfulSearchTerminationNode
                                           , queryPartialTestVectorRepresentation
                            }
                  | WildcardNode
                    {
                        SubtreeWithAllLevelsForSameTestVariableIndex = subtreeWithAllLevelsForSameTestVariableIndex
                        SubtreeForFollowingIndices = subtreeForFollowingIndices
                    }
                    , ((Some levelFromQueryPartialTestVectorRepresentation) as headFromQueryPartialTestVectorRepresentation) :: tailFromQueryPartialTestVectorRepresentation ->
                        continuationWorkflow
                            {
                                let! modifiedSubtreeWithAllLevelsForSameTestVariableIndex
                                     , removedPartialTestVector =
                                    removeLevelFromBinaryTreeOfLevelsForTestVariable subtreeWithAllLevelsForSameTestVariableIndex
                                                                                     levelFromQueryPartialTestVectorRepresentation
                                                                                     tailFromQueryPartialTestVectorRepresentation
                                let modifiedTernarySearchTree =
                                    buildResultSubtreeFromWildcardNodeWithPruningOfDegenerateLinearSubtrees modifiedSubtreeWithAllLevelsForSameTestVariableIndex
                                                                                                            subtreeForFollowingIndices
                                return modifiedTernarySearchTree
                                       , removedPartialTestVector
                            }
                        + buildResultFromWildcardNodeModifyingSubtreeForFollowingTestVariableIndices headFromQueryPartialTestVectorRepresentation
                                                                                                     tailFromQueryPartialTestVectorRepresentation
                                                                                                     subtreeWithAllLevelsForSameTestVariableIndex
                                                                                                     subtreeForFollowingIndices
                  | WildcardNode
                    {
                        SubtreeWithAllLevelsForSameTestVariableIndex = subtreeWithAllLevelsForSameTestVariableIndex
                        SubtreeForFollowingIndices = subtreeForFollowingIndices
                    }
                    , None :: tailFromQueryPartialTestVectorRepresentation ->
                        continuationWorkflow
                            {
                                let! modifiedSubtreeWithAllLevelsForSameTestVariableIndex
                                     , removedPartialTestVector =
                                    removeWildcardLevelFromBinaryTreeOfLevelsForTestVariable subtreeWithAllLevelsForSameTestVariableIndex
                                                                                             tailFromQueryPartialTestVectorRepresentation
                                let modifiedTernarySearchTree =
                                    buildResultSubtreeFromWildcardNodeWithPruningOfDegenerateLinearSubtrees modifiedSubtreeWithAllLevelsForSameTestVariableIndex
                                                                                                            subtreeForFollowingIndices
                                return modifiedTernarySearchTree
                                       , removedPartialTestVector
                            }
                        + buildResultFromWildcardNodeModifyingSubtreeForFollowingTestVariableIndices None
                                                                                                     tailFromQueryPartialTestVectorRepresentation
                                                                                                     subtreeWithAllLevelsForSameTestVariableIndex
                                                                                                     subtreeForFollowingIndices
                  | BinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
                    , Some levelFromQueryPartialTestVectorRepresentation :: tailFromQueryPartialTestVectorRepresentation ->
                        removeLevelFromBinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
                                                                         levelFromQueryPartialTestVectorRepresentation
                                                                         tailFromQueryPartialTestVectorRepresentation
                       |> adaptResultToRemovalFromTernaryTree
                  | BinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
                    , None :: tailFromQueryPartialTestVectorRepresentation ->
                        removeWildcardLevelFromBinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
                                                                                 tailFromQueryPartialTestVectorRepresentation
                        |> adaptResultToRemovalFromTernaryTree
                  | _
                    , [] ->
                        // This has the effect of padding out a query partial test vector on the fly, thereby
                        // allowing a match as a prefix of some suitable stored vector, should one already be present.
                        removeFromTernarySearchTree ternarySearchTree
                                                    [None]
                                                    treeSearchContextParameters
            removeFromTernarySearchTree ternarySearchTree
                                        queryPartialTestVectorRepresentation
                                        TreeSearchContextParameters.StartOfSearch

        let checkInvariant ternarySearchTree =
            let rec checkInvariantOfTernarySearchTree ternarySearchTree
                                                      lowerBound
                                                      upperBound
                                                      testVariableIndex =
                let rec checkInvariantOfBinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
                                                                          lowerBound
                                                                          upperBound =
                    match binaryTreeOfLevelsForTestVariable with
                        UnsuccessfulSearchTerminationNode ->
                            if maximumNumberOfTestVariables <= testVariableIndex
                            then
                                raise (InternalAssertionViolationException "The test vector refers to test variable indices that are greater than the permitted maximum.")

                            0u
                      | AugmentedInternalNode
                        (InternalNode
                        {
                            LevelForTestVariableIndex = levelForTestVariableIndex
                            SubtreeWithLesserLevelsForSameTestVariableIndex = subtreeWithLesserLevelsForSameTestVariableIndex
                            SubtreeWithGreaterLevelsForSameTestVariableIndex = subtreeWithGreaterLevelsForSameTestVariableIndex
                            SubtreeForFollowingIndices = subtreeForFollowingIndices
                        }) ->
                            let liftedLevel =
                                Finite levelForTestVariableIndex
                            if liftedLevel >= upperBound
                            then
                                raise (InvariantViolationException "Level is greater than or equal to exclusive upper bound.")
                            if liftedLevel <= lowerBound
                            then
                                raise (InvariantViolationException "Level is less than or equal to exclusive lower bound.")
                            let numberOfSuccessfulPathsFromSubtreeWithLesserLevelsForSameTestVariableIndex =
                                checkInvariantOfBinaryTreeOfLevelsForTestVariable subtreeWithLesserLevelsForSameTestVariableIndex
                                                                                  lowerBound
                                                                                  liftedLevel
                            let numberOfSuccessfulPathsFromSubtreeWithGreaterLevelsForSameTestVariableIndex =
                                checkInvariantOfBinaryTreeOfLevelsForTestVariable subtreeWithGreaterLevelsForSameTestVariableIndex
                                                                                  liftedLevel
                                                                                  upperBound
                            let numberOfSuccessfulPathsFromSubtreeForFollowingIndices =
                                checkInvariantOfTernarySearchTree subtreeForFollowingIndices
                                                                  NegativeInfinity
                                                                  PositiveInfinity
                                                                  (testVariableIndex + 1u)
                            match numberOfSuccessfulPathsFromSubtreeWithLesserLevelsForSameTestVariableIndex
                                  , numberOfSuccessfulPathsFromSubtreeWithGreaterLevelsForSameTestVariableIndex
                                  , numberOfSuccessfulPathsFromSubtreeForFollowingIndices with
                                0u
                                , 0u
                                , 0u ->
                                    raise (InvariantViolationException "Redundant internal node with no successful search paths leading through it.")
                              | _
                                , 0u
                                , 0u ->
                                    raise (InvariantViolationException "Redundant internal node with all successful search paths leading via subtree for lesser levels.")
                              | 0u
                                , _
                                , 0u ->
                                    raise (InvariantViolationException "Redundant internal node with all successful search paths leading via subtree for greater levels.")
                              | _
                                , _
                                , 0u ->
                                    raise (InvariantViolationException "Redundant internal node with its own 'ghost' level that participates in no successful search paths.")
//                              | 0u
//                                , _
//                                , _ ->
//                                    if numberOfSuccessfulPathsFromSubtreeWithGreaterLevelsForSameTestVariableIndex > 1u
//                                    then
//                                        Diagnostics.Debug.Print ("Lone greater subtree with: {0} successful paths through it.", numberOfSuccessfulPathsFromSubtreeWithGreaterLevelsForSameTestVariableIndex)
//                                    numberOfSuccessfulPathsFromSubtreeWithGreaterLevelsForSameTestVariableIndex
//                                    + numberOfSuccessfulPathsFromSubtreeForFollowingIndices
//                              | _
//                                , 0u
//                                , _ ->
//                                    if numberOfSuccessfulPathsFromSubtreeWithLesserLevelsForSameTestVariableIndex > 1u
//                                    then
//                                        Diagnostics.Debug.Print ("Lone lesser subtree with: {0} successful paths through it.", numberOfSuccessfulPathsFromSubtreeWithLesserLevelsForSameTestVariableIndex)
//                                    numberOfSuccessfulPathsFromSubtreeWithLesserLevelsForSameTestVariableIndex
//                                    + numberOfSuccessfulPathsFromSubtreeForFollowingIndices
                              | _ ->
                                    numberOfSuccessfulPathsFromSubtreeWithLesserLevelsForSameTestVariableIndex
                                    + numberOfSuccessfulPathsFromSubtreeWithGreaterLevelsForSameTestVariableIndex
                                    + numberOfSuccessfulPathsFromSubtreeForFollowingIndices
                match ternarySearchTree with
                    SuccessfulSearchTerminationNode ->
                        if maximumNumberOfTestVariables < testVariableIndex  // NOTE: a subtlety - remember that 'testVariableIndex' can reach 'maximumNumberOfTestVariablesOverall'
                                                                             // for a full (and possibly removed) test vector, because successful searches go through at least one
                                                                             // node corresponding to each test variable index, *then* land on a node indicating whether the search
                                                                             // was successful or not: so the zero-relative index gets incremented one more time.
                        then
                            raise (InternalAssertionViolationException "The test vector refers to test variable indices that are greater than the permitted maximum.")

                        1u
                  | BinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable ->
                        checkInvariantOfBinaryTreeOfLevelsForTestVariable binaryTreeOfLevelsForTestVariable
                                                                          lowerBound
                                                                          upperBound
                  | WildcardNode
                    {
                        SubtreeWithAllLevelsForSameTestVariableIndex = subtreeWithAllLevelsForSameTestVariableIndex
                        SubtreeForFollowingIndices = subtreeForFollowingIndices
                    } ->
                        let numberOfSuccessfulPathsFromSubtreeWithAllLevelsForSameTestVariableIndex =
                            checkInvariantOfBinaryTreeOfLevelsForTestVariable subtreeWithAllLevelsForSameTestVariableIndex
                                                                              lowerBound
                                                                              upperBound
                        let numberOfSuccessfulPathsFromSubtreeForFollowingIndices =
                            checkInvariantOfTernarySearchTree subtreeForFollowingIndices
                                                              NegativeInfinity
                                                              PositiveInfinity
                                                              (testVariableIndex + 1u)
                        match numberOfSuccessfulPathsFromSubtreeWithAllLevelsForSameTestVariableIndex
                              , numberOfSuccessfulPathsFromSubtreeForFollowingIndices with
                            0u
                            , 0u ->
                                raise (LogicErrorException "Redundant wildcard node with no successful search paths leading through it.")
                          | _
                            , 0u ->
                                raise (LogicErrorException "Redundant wildcard node that has no successful paths using its wildcard match leading through it.")
                          | _
                            , _ ->
                                numberOfSuccessfulPathsFromSubtreeWithAllLevelsForSameTestVariableIndex
                                + numberOfSuccessfulPathsFromSubtreeForFollowingIndices

            if 0u = checkInvariantOfTernarySearchTree ternarySearchTree
                                                      NegativeInfinity
                                                      PositiveInfinity
                                                      0u
            then
                raise (LogicErrorException "No successful search paths but tree should be non-empty.")

        member this.MaximumNumberOfTestVariables =
            maximumNumberOfTestVariables

        member this.EnumerationOfMergedTestVectors revealFullTestVectorsAgain =
            createPartialTestVectorSequence revealFullTestVectorsAgain

        static member Initial maximumNumberOfTestVariablesOverall =
            MergedPartialTestVectorRepresentations<'Level> (SuccessfulSearchTerminationNode,
                                                            maximumNumberOfTestVariablesOverall)

        member this.MergeOrAdd partialTestVectorRepresentationInExternalForm =
            if Map.isEmpty partialTestVectorRepresentationInExternalForm
            then
                this
                , None
            else
                let partialTestVectorRepresentation
                    , partialTestVectorRepresentationIsActuallyAlreadyFull =
                    fillOutPartialTestVectorWithIndeterminates partialTestVectorRepresentationInExternalForm
                let detectAndConvertFullTestVector partialTestVectorRepresentation =
                    let lengthOfPartialTestVectorRepresentation =
                        List.length partialTestVectorRepresentation
                        |> uint32

                    if lengthOfPartialTestVectorRepresentation > maximumNumberOfTestVariables
                    then
                        raise (InternalAssertionViolationException "The merged removed partial test vector has more entries than the permitted maximum number of test variables.")

                    if lengthOfPartialTestVectorRepresentation
                        |> uint32 < maximumNumberOfTestVariables
                        || partialTestVectorRepresentation
                            |> List.exists Option.isNone
                    then
                        None
                    else
                        let testVariableIndicesForFullTestVector =
                            List.init (int32 maximumNumberOfTestVariables)
                                        uint32

                        let testVariableLevelsForFullTestVector =
                            partialTestVectorRepresentation
                            |> List.map Option.get

                        List.zip testVariableIndicesForFullTestVector
                                    testVariableLevelsForFullTestVector
                        |> Map.ofList
                        |> Some
                let modifiedTernarySearchTree
                    , fullTestVectorBeingOfferedNowForEarlyAccess =
                    ContinuationMonad<_, _>.CallCC ((fun () ->
                                                        continuationWorkflow
                                                            {
                                                                return ternarySearchTree
                                                                       , None
                                                            }),
                                                    (fun existingFullTestVectorBlockedRemovalContinuation ->
                                                        continuationWorkflow
                                                            {
                                                                let! ternarySearchTreeWithoutMergeCandidate
                                                                     , mergedPartialTestVectorRepresentation =
                                                                    remove ternarySearchTree
                                                                           partialTestVectorRepresentation
                                                                           existingFullTestVectorBlockedRemovalContinuation
                                    //                            match remove ternarySearchTreeWithoutMergeCandidate
                                    //                                            mergedPartialTestVectorRepresentation with
                                    //                                Some _ ->
                                    //                                    raise (InternalAssertionViolationException "The merged removed partial test vector still matches with something left behind!")
                                    //                                | _ ->
                                    //                                    ()


                                                                return add ternarySearchTreeWithoutMergeCandidate
                                                                           mergedPartialTestVectorRepresentation
                                                                       , detectAndConvertFullTestVector mergedPartialTestVectorRepresentation
                                                            }))
                    + continuationWorkflow
                        {
                            return add ternarySearchTree
                                       partialTestVectorRepresentation
                                   , if partialTestVectorRepresentationIsActuallyAlreadyFull
                                        then
                                            Some partialTestVectorRepresentationInExternalForm
                                        else
                                            None
                        }
                    |> ContinuationMonad<_, _>.Execute
//                if 7 = (hash this) % 100
//                then
//                    // Invariant check...
//                    checkInvariant modifiedTernarySearchTree
//                    // ... end of invariant check.

                MergedPartialTestVectorRepresentations (modifiedTernarySearchTree,
                                                        maximumNumberOfTestVariables)
                , fullTestVectorBeingOfferedNowForEarlyAccess
