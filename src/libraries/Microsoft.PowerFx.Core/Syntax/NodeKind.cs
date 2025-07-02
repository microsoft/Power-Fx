// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Syntax
{   
    /// <summary>
    /// Represents the various kinds of nodes used in the syntax tree.
    /// </summary>
    /// <remarks>This enumeration is primarily used by IntelliSense and other tools to classify and work with 
    /// different types of nodes in the syntax tree. Each value corresponds to a specific kind of  syntax element, such
    /// as literals, operators, or structural elements.</remarks>
    public enum NodeKind
    {
        /// <summary>
        /// Represents a blank or placeholder type or member.
        /// </summary>
        Blank,

        /// <summary>
        /// Represents a binary operation that combines two operands using a specified operator.
        /// </summary>
        /// <remarks>This type is typically used to model operations involving two inputs, such as
        /// addition, subtraction,  multiplication, or other binary operations. The specific behavior of the operation
        /// depends on the  implementation or context in which this type is used.</remarks>
        BinaryOp,

        /// <summary>
        /// Represents a unary operation in an expression tree or similar structure.
        /// </summary>
        /// <remarks>A unary operation is an operation with only one operand, such as negation or
        /// increment. This class can be used to model and evaluate such operations in various contexts.</remarks>
        UnaryOp,

        /// <summary>
        /// Represents an operation that can accept a variable number of arguments.
        /// </summary>
        /// <remarks>This class is designed to handle operations where the number of inputs is not fixed.
        /// It provides flexibility for scenarios requiring variadic behavior, such as mathematical operations or
        /// aggregations over a dynamic set of inputs.</remarks>
        VariadicOp,

        /// <summary>
        /// Represents a method or action to be invoked.
        /// </summary>
        /// <remarks>This member is a placeholder and should be implemented or extended to define specific
        /// behavior.</remarks>
        Call,

        /// <summary>
        /// Represents a collection of objects that can be accessed by index. Provides methods to search, sort, and
        /// manipulate lists.
        /// </summary>
        /// <summary>
        /// Represents a collection of objects that can be accessed by index. Provides methods to search, sort, and
        /// manipulate lists.
        /// </summary>
        /// <remarks>The <see cref="System.Collections.Generic.List{T}"/> class provides a dynamic array implementation that
        /// automatically resizes as elements are added or removed. It is not thread-safe; if multiple threads access a
        /// <see cref="System.Collections.Generic.List{T}"/> instance concurrently, synchronization is required.</remarks>
        List,

        /// <summary>
        /// Represents a record containing structured data.
        /// </summary>
        /// <remarks>This class is typically used to store and manipulate data in a structured format. It
        /// can be extended or used as-is depending on the application's requirements.</remarks>
        Record,

        /// <summary>
        /// Represents a table structure that can store and organize data in rows and columns.
        /// </summary>
        /// <remarks>This class provides functionality to manage tabular data, including adding, removing,
        /// and accessing rows and columns. It can be used in scenarios where structured data  needs to be represented
        /// in a tabular format.</remarks>
        Table,

        /// <summary>
        /// Represents a name composed of multiple segments separated by dots.
        /// </summary>
        /// <remarks>This class is typically used to represent hierarchical or structured names, such as
        /// namespaces, file paths, or configuration keys. Each segment of the name is separated by a dot ('.')
        /// character.</remarks>
        DottedName,

        /// <summary>
        /// Gets or sets the first name of the individual.
        /// </summary>
        FirstName,

        /// <summary>
        /// Represents a placeholder for future implementation or functionality.
        /// </summary>
        /// <remarks>This class is currently not implemented and serves as a placeholder.  Future versions
        /// may include additional functionality or behavior.</remarks>
        As,

        /// <summary>
        /// Represents the parent entity in a hierarchical relationship.
        /// </summary>
        /// <remarks>This class serves as the base or parent entity in scenarios where hierarchical or 
        /// parent-child relationships are modeled. It can be extended or used directly  depending on the specific
        /// requirements of the application.</remarks>
        Parent,

        /// <summary>
        /// Represents the current instance of the object.
        /// </summary>
        /// <remarks>This property is typically used to return the current instance of the object in
        /// contexts where a reference to itself is required.</remarks>
        Self,

        /// <summary>
        /// Represents a boolean literal value.
        /// </summary>
        /// <remarks>This type is used to encapsulate a boolean value, such as <see langword="true"/> or
        /// <see langword="false"/>,  in contexts where a literal representation is required.</remarks>
        BoolLit,

        /// <summary>
        /// Represents a numeric literal in the context of the application.
        /// </summary>
        /// <remarks>This type is typically used to encapsulate and work with numeric values that are
        /// treated as literals in a specific domain or processing context.</remarks>
        NumLit,

        /// <summary>
        /// Represents a string literal in the context of the application.
        /// </summary>
        /// <remarks>This type is typically used to encapsulate and work with string literals in scenarios
        /// where additional processing or metadata may be required.</remarks>
        StrLit,

        /// <summary>
        /// Represents a decimal literal token in the syntax tree.
        /// </summary>
        /// <remarks>This token is typically used to represent numeric values in decimal format within a
        /// parsed syntax structure.</remarks>
        DecLit,

        /// <summary>
        /// Represents an error that occurs during application execution.
        /// </summary>
        /// <remarks>This class can be used to encapsulate error details, such as error messages or codes,
        /// that occur during the execution of the application. It is intended to provide a  standardized way to handle
        /// and propagate errors.</remarks>
        Error,

        /// <summary>
        /// Provides functionality for performing string interpolation with custom formatting.
        /// </summary>
        /// <remarks>This class is designed to simplify the process of creating formatted strings by
        /// interpolating values into a template. It supports custom formatting options and can be extended for specific
        /// use cases.</remarks>
        StrInterp,

        /// <summary>
        /// Represents a literal type in the system, typically used to define or reference a specific type at runtime.
        /// </summary>
        /// <remarks>This class is commonly used in scenarios where type information needs to be
        /// explicitly represented or manipulated. It can be useful in reflection-based operations or dynamic type
        /// handling.</remarks>
        TypeLiteral
    }
}
