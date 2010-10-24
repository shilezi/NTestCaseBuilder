#light

namespace SageSerpent.TestInfrastructure

    open System.Collections
    
    /// <summary>Test case enumerable factories form a composite data structure, where a factory is the root
    /// of a tree of simpler factories, and can itself be part of a larger tree (or even part of several trees,
    /// as sharing is permitted). Ultimately a valid tree of test case enumerable factories will have leaf node
    /// factories based on sequences of test variable levels: each leaf node factory produces a trivial
    /// sequence of test cases that are just the levels of its own test variable.
    
    /// Factories that are internal nodes in the tree belong to two types. The first type is a synthesizing
    /// factory: it creates a sequence of synthesized test cases, where each synthesized test case is created
    /// from several simpler input test cases taken from across distinct sequences provided by corresponding
    /// subtrees of the synthesizing factory. The second type is an interleaving factory: it creates a sequence
    /// by interleaving the sequences provided by the subtrees of the interleaving factory.
    
    /// The crucial idea is that for any factory at the head of some subtree, then given a test variable
    /// combination strength, a sequence of test cases can be created that satisfies the property that every
    /// combination of that strength of levels from distinct test variables represented by the leaves exists
    /// in at least one of the test cases in the sequence, provided the following conditions are met:-
    /// 1. The number of distinct test variables is at least the requested combination strength (obviously).
    /// 2. For any combination in question above, the test variables that contribute the levels being combined
    /// all do so via paths up to the head of the tree that fuse together at synthesizing factories and *not*
    /// at interleaving factories.
    
    /// So for example, creating a sequence of strength two for a tree composed solely of synthesizing factories
    /// for the internal nodes and test variable leaf nodes would guarantee all-pairs coverage for all of the
    /// test variables.</summary>
    
    /// <remarks>1. Levels belonging to the same test variable are never combined together via any of the
    /// internal factory nodes.</remarks>
    /// <remarks>2. Sharing a single sequence of test variable levels between different leaf node factories
    /// in a tree (or sharing tree node factories to create a DAG) does not affect the strength guarantees:
    /// any sharing of levels or nodes is 'expanded' to create the same effect as an equivalent tree structure
    /// containing duplicated test variables and levels.</remarks>
    /// <remarks>3. If for a combination of levels from distinct test variables, the test variables that
    /// contribute the levels being combined all do so via paths up to the head of the tree that fuse together
    /// at interleaving factories, then that combination *will not* occur in any test case in any sequence
    /// generated by the head of the tree.</remarks>
    /// <remarks>4. If some test variables can only be combined in strengths less than the requested strength (due
    /// to interleaving between some of the test variables), then the sequence will contain combinations of these
    /// variables in the maximum possible strength.
    /// <remarks>5. If there are insufficient test variables to make up the combination strength, or there are
    /// enough test variables but not enough that are combined by synthesizing factories, then the sequence
    /// will 'do its best' by creating combinations of up to the highest strength possible, falling short of the
    /// requested strength.
    /// <remarks>6. A factory only makes guarantees as to the strength of combination of levels that contribute
    /// via synthesis to a final test case: so if for example a synthesizing factory causes 'collisions' to occur between
    /// several distinct combinations of simpler test cases by creating the same output test case for all of those
    /// combinations, then no attempt will be made to work around this behaviour and try alternative combinations that
    /// satisfy the strength requirements but create fewer collisions. However, a factory takes an unsigned
    /// integer parameter to create a test case enumerable: if there are too many collisions in the enumerable's sequence
    /// of test cases, then the client can retry with a different enumerable created using a different parameter value.

    type ITestCaseEnumerableFactory =
        inherit INodeWrapper
        
        abstract member CreateEnumerable: uint32 -> IEnumerable;
        
        abstract member MaximumStrength: System.UInt32;
