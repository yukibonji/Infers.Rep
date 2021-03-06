// Copyright (C) by Vesa Karvonen

/// Infers.Rep is a library providing inference rules for polytypic or datatype
/// generic programming with the Infers library.
///
/// Infers.Rep uses reflection and run-time code generation to build type
/// representations for various F# types.  Those type representations can be
/// accessed using Infers by writing rules over the structure of types.  The
/// type representations provided by Infers.Rep make it possible to manipulate
/// values of the represented types efficiently: after the type representation
/// has been created, no further use of slow reflection, boxing or other kinds
/// of auxiliary memory allocations are required.
namespace Infers.Rep

open Infers

////////////////////////////////////////////////////////////////////////////////

/// Represents an empty product as a special case for union cases.
type Empty = struct end

/// Represents a pair of the types `'e` and `'r`.
#if DOC
///
/// Note that the idea behind using a struct type is to make it possible to
/// construct and deconstruct products without performing any heap allocations.
/// When used carefully, avoiding copying and making sure structs are stack
/// allocated or directly embedded within objects, this can lead to
/// significantly better performance than with heap allocated products.
/// However, naive use results in both heap allocations and copying, which can
/// lead to worse performance than with heap allocated products.
///
/// Note that while it is in no way enforced, the idea is that in a nested
/// product the `Elem` field is the current singleton element and `Rest` is
/// the remainder of the nested produced.  For example, the nested product
/// of the type
///
///> char * int * float * bool
///
/// would be
///
///> Pair<char, Pair<int, Pair<float, bool>>>
///
/// The `Rep` rules generate products in this manner and it is important to
/// understand this in order to write rules guided by nested pairs.
#endif
type [<Struct>] Pair<'e,'r> =
  /// The current element.
  val mutable Elem: 'e

  /// The remainder of the product.
  val mutable Rest: 'r

  /// Constructs a pair.
  new: 'e * 'r -> Pair<'e,'r>

[<AutoOpen>]
module Pair =
  /// Active pattern for convenient (but slow) matching of pair structs.
  val inline (|Pair|): Pair<'e,'r> -> 'e * 'r

////////////////////////////////////////////////////////////////////////////////

/// Base class for `AsChoices<'s,'t>` that does not include the `'s` type
/// parameter to allow it to be ignored.
type [<AbstractClass>] AsChoices<'t> =
  inherit Rules
  new: unit -> AsChoices<'t>

  /// The number of cases the union type `'t` has.
  val Arity: int

  /// Returns the integer tag of the given union value.
  abstract Tag: 't -> int

/// Representation of the type `'t` as nested choices of type `'s`.
#if DOC
///
/// An `AsChoices<'s,'t>` class generated by Infers.Rep also contains a rule of
/// the form
///
///> _: Case<'p,'o,'t>
///
/// where `'p` is a representation of the case as nested pairs and `'o` is a
/// nested choice that identifies the particular case.
#endif
type [<AbstractClass>] AsChoices<'s,'t> =
  inherit AsChoices<'t>
  new: unit -> AsChoices<'s,'t>

//  abstract ToSum: 'u -> 'c
//  abstract OfSum: 'c -> 'u

////////////////////////////////////////////////////////////////////////////////

/// Base class for type representations.
type [<AbstractClass>] Rep<'t> =
  inherit Rules
  new: unit -> Rep<'t>

////////////////////////////////////////////////////////////////////////////////

/// Representation for primitive types.
type [<AbstractClass>] Prim<'t> =
  inherit Rep<'t>
  new: unit -> Prim<'t>

////////////////////////////////////////////////////////////////////////////////

/// Representation for enumerated types.
type [<AbstractClass>] Enum<'t> =
  inherit Rep<'t>
  new: unit -> Enum<'t>

/// Representation for enumerated type `'t` whose underlying type is `'u`.
type [<AbstractClass>] Enum<'u,'t> =
  inherit Enum<'t>
  new: unit -> Enum<'u,'t>

////////////////////////////////////////////////////////////////////////////////

/// Representation for "subtyped" .Net types.
type [<AbstractClass>] Subtyped<'t> =
  inherit Rep<'t>
  new: unit -> Subtyped<'t>

/// Representation for struct or value types.
type [<AbstractClass>] Struct<'t> =
  inherit Subtyped<'t>
  new: unit -> Struct<'t>

/// Representation for class types.
type [<AbstractClass>] Class<'t> =
  inherit Subtyped<'t>
  new: unit -> Class<'t>

/// Representation for interface types.
type [<AbstractClass>] Interface<'t> =
  inherit Subtyped<'t>
  new: unit -> Interface<'t>

////////////////////////////////////////////////////////////////////////////////

/// Type representation for the F# product type (tuple or record) `'t`.
#if DOC
///
/// A `Product<'t>` class generated by Infers.Rep also contains a rule of the
/// form
///
///> _: AsPairs<'p,'t,'t>
///
/// where the type `'p` is a representation of the product as nested pairs.
///
/// See also `Union<'t>`.
#endif
type [<AbstractClass>] Product<'t> =
  inherit Rep<'t>
  new: unit -> Product<'t>

/// Abstract representation of an element of type `'e` of the product type `'t`.
type [<AbstractClass>] Elem<'e,'t> =
  new: unit -> Elem<'e,'t>

  /// The index of the element.
  val Index: int

  /// Returns the value of the element.
  abstract Get: 't -> 'e

/// Unique representation of an element of type `'e` of the product type `'t`.
type [<AbstractClass>] Elem<'e,'r,'o,'t> =
  inherit Elem<'e,'t>
  new: unit -> Elem<'e,'r,'o,'t>

/// Representation of a possibly labelled element of type `'e`.
type [<AbstractClass>] Labelled<'e,'r,'o,'t> =
  inherit Elem<'e,'r,'o,'t>
  new: unit -> Labelled<'e,'r,'o,'t>
  
  /// The name of the label.
  val Name: string

////////////////////////////////////////////////////////////////////////////////

/// Type representation for the F# tuple type `'t`.
type [<AbstractClass>] Tuple<'t> =
  inherit Product<'t>
  new: unit -> Tuple<'t>

/// Representation of an element of type `'e` of a tuple of type `'t`.
type [<AbstractClass>] Item<'e,'r,'t> =
  inherit Elem<'e,'r,'t,'t>
  new: unit -> Item<'e,'r,'t>

////////////////////////////////////////////////////////////////////////////////

/// Type representation for the F# record type `'t`.
type [<AbstractClass>] Record<'t> =
  inherit Product<'t>
  new: unit -> Record<'t>

/// Representation of a field of type `'e` of the record type `'t`.
type [<AbstractClass>] Field<'e,'r,'t> =
  inherit Labelled<'e,'r,'t,'t>
  new: unit -> Field<'e,'r,'t>

  /// Whether the field is mutable.
  val IsMutable: bool

  /// Sets the value of the field assuming this is a mutable field.
  abstract Set: 't * 'e -> unit

////////////////////////////////////////////////////////////////////////////////

/// Base class for `AsPairs<'p,'o,'t>` that does not include the `'o` type
/// parameter to allow it to be ignored.
type [<AbstractClass>] AsPairs<'p,'t> =
  inherit Rules
  new: unit -> AsPairs<'p,'t>

  /// The number of elements the product type has.
  val Arity: int

  /// Whether the product type directly contains mutable fields.
  val IsMutable: bool

  /// Copies the fields of the type `'t` to the generic product of type `'p`.
  abstract Extract: from: 't * into: byref<'p> -> unit

  /// Creates a new instance of type `'t` from the nested pairs of type `'p`.
  abstract Create: from: byref<'p> -> 't

  /// Overwrites the fields of the record type `'t` with values from the nested
  /// pairs of type `'p`.  Along with `Default` this supports the generation of
  /// cyclic records.
  abstract Overwrite: Record<'t> * into: 't * from: byref<'p> -> unit

  /// Convenience function to convert from product type to nested pairs.
  abstract ToPairs: 't -> 'p

  /// Convenience function to convert from nested pairs to product type.
  abstract OfPairs: 'p -> 't

  /// Convenience function to create a new default valued (all default values)
  /// object of the record type `'t`.  Along with `Overwrite` this supports the
  /// generation of cyclic records.
  abstract Default: Record<'t> -> 't

/// Representation of the type `'t` as nested pairs of type `'p`.
#if DOC
///
/// An `AsPairs<'p,'o,'t>` class generated by Infers.Rep also contains rules for
/// accessing the elements of the product.  Depending on the type `'t` those
/// rules are of one of the following forms:
///
///> _:  Item<e,r,  t>                      :> Elem<e,r,t,t> :> Elem<e,t>
///> _: Label<e,r,o,t> :> Labelled<e,r,o,t> :> Elem<e,r,o,t> :> Elem<e,t>
///> _: Field<e,r,  t> :> Labelled<e,r,o,t> :> Elem<e,r,t,t> :> Elem<e,t>
#endif
type [<AbstractClass>] AsPairs<'p,'o,'t> =
  inherit AsPairs<'p,'t>
  new: unit -> AsPairs<'p,'o,'t>

////////////////////////////////////////////////////////////////////////////////

/// Type representation for the F# union type `'t`.
#if DOC
///
/// A `Union<'t>` class generated by Infers.Rep also contains a rule of the form
///
///> _: AsChoices<'s,'t>
///
/// where type `'s` is a representation of the union as nested binary choices.
///
/// Note that while union types are not considered as product types in
/// Infers.Rep, one can view a union type with only a single case as a product.
/// For example,
///
///> type foo = Bar of int * string * float
///
/// can be viewed as a product
///
///> AsPairs<Pair<int, Pair<string, float>>,
///>         Pair<int, Pair<string, float>>,
///>         foo>
///
/// and a rule for this is provided directly by Infers.Rep.  If you need to
/// handle product types and union types separately, say in a pretty printing
/// generic, you should have the `Union<_>` and `Product<_>` predicates in your
/// rules.
#endif
type [<AbstractClass>] Union<'t> =
  inherit Rep<'t>
  new: unit -> Union<'t>

/// Representation of a case of the F# union type `'t`.
type [<AbstractClass>] Case<'p,'o,'t> =
  inherit AsPairs<'p,'o,'t>
  new: unit -> Case<'p,'o,'t>

  /// The name of the case.
  val Name: string

  /// The integer tag of the case.
  val Tag: int

/// Representation of a possibly labelled element of type `'e` of a case of the
/// F# union type `'t`.
type [<AbstractClass>] Label<'e,'r,'o,'t> =
  inherit Labelled<'e,'r,'o,'t>
  new: unit -> Label<'e,'r,'o,'t>
