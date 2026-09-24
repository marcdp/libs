# X Template Language Specification

**Document status:** Normative XTemplate language specification  
**Language name:** X Template / XTemplate / XTL  
**Primary implementation:** XShell `x` render engine  
**Purpose:** Human- and machine-readable specification suitable as the basis for implementing parsers, interpreters, compilers, renderers, validators, and conformance tests.

---

## 1. Purpose

X Template (XTL) is the declarative template language used by the XShell `x` render engine.

An X Template is an HTML fragment extended with:

- restricted XTemplate expressions;
- text interpolation;
- dynamic attributes and properties;
- event-to-command bindings;
- conditional rendering;
- list rendering;
- keyed list reconciliation;
- recursive tree rendering;
- conditional CSS classes;
- two-way model binding;
- raw HTML and raw DOM-node insertion;
- one-time rendering;
- literal/preformatted subtrees.

The language is intentionally small. It does not define components, state management, routing, dependency injection, or module packaging. Those belong to other XShell layers.

This document is the **normative source of truth** for the XTemplate language. It defines the template language contract independently of any
particular compiler implementation. It also documents the current XShell virtual-DOM ABI where that information is necessary to implement a
compatible compiler.

The other documents in this section are explanatory or implementation-oriented guides derived from this specification. If they differ from this
document, this document takes precedence. See [X Templates](index.md) for the documentation map.

---

## 2. Sources used to derive this specification

This specification is based on the current XShell implementation under:

```text
src/DProjects.XShell/**
```

In particular:

```text
Resources/DProjects.XShell/modules/x/controllers/x-template.js
Resources/DProjects.XShell/xshell/render-engines/x.js
Resources/DProjects.XShell/modules/x/controllers/x-element.js
Resources/DProjects.XShell/docs/extensions/x-templates/*
Resources/DProjects.XShell/modules/x/components/*
Resources/DProjects.XShell/modules/x/pages/*
Resources/DProjects.XShell/modules/x/layouts/*
```

The existing implementation is the behavioral reference, but this document deliberately distinguishes:

1. **language semantics** — behavior implementations should preserve;
2. **runtime ABI details** — behavior required only when targeting the current XShell VDOM renderer;
3. **implementation quirks or apparent defects** — behavior that should not automatically become part of the language.

---

## 3. Normative terminology

The keywords **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, and **MAY** are normative.

A **conforming template** is a template that follows the syntax and structural rules in this specification.

A **conforming implementation** is a compiler, interpreter, renderer, or validator that implements the observable semantics defined here.

An implementation targeting the current XShell VDOM runtime must additionally follow the ABI rules in the VDOM compatibility section.

---

## 4. High-level execution model

Conceptually:

```text
X Template source
    ↓
template parser
    ↓
template AST
    ↓
expression parser
    ↓
expression AST
    +----------------------+----------------------+----------------------+
    |                      |                      |
JavaScript compiler   C# server renderer   validation/tooling
```

The template parser owns template structure. Every expression-bearing construct is tokenized and parsed into an implementation-neutral expression AST.
A backend may emit code from that AST or evaluate it directly.

An implementation may use:

- direct DOM interpretation;
- an AST interpreter;
- JavaScript source generation;
- bytecode;
- a virtual DOM;
- DOM parts;
- server-side HTML rendering;
- another backend.

The language itself is not defined in terms of a particular backend.

The current XShell implementation compiles XTL into a JavaScript render function that produces XShell virtual nodes.

---

# Part I — Source Language

## 5. Template source is an HTML fragment

An X Template is parsed as an **HTML fragment**, not XML.

A conforming browser-oriented implementation SHOULD behave like assigning the source to:

```js
const template = document.createElement("template");
template.innerHTML = source;
```

Consequences include normal HTML parsing behavior:

- HTML element names are effectively case-insensitive;
- HTML attribute names are effectively case-insensitive;
- comments become comment nodes;
- whitespace text nodes are preserved according to HTML parsing;
- normal HTML content-model rules apply;
- custom elements follow HTML parsing rules, not XML rules.

Implementers should not parse XTL with an XML parser unless they deliberately reproduce HTML-fragment behavior.

### 5.1 Leading and trailing whitespace

The current reference compiler trims the complete template string before HTML parsing.

Therefore:

```text
template.trim()
```

is part of current observable behavior for string templates.

Whitespace **inside** the outer boundaries remains significant and may produce text nodes.

---

## 6. Text interpolation

XTL supports text interpolation with double braces:

```html
Hello {{ state.name }}
```

The expression between `{{` and `}}` is evaluated at render time and converted to text.

Equivalent conceptual form:

```html
Hello <x:text>state.name</x:text>
```

The reference compiler currently implements interpolation by replacing:

```text
{{  →  <x:text>
}}  →  </x:text>
```

before HTML parsing.

### 6.1 Interpolation is a text construct

Interpolation SHOULD be used in text-node positions:

```html
<p>Hello {{ state.name }}</p>
```

Attribute interpolation is not part of XTL:

```html
<!-- NOT XTL dynamic binding -->
<div title="{{ state.title }}"></div>
```

Use:

```html
<div x-attr:title="state.title"></div>
```

instead.

### 6.2 Conversion

The result is converted with the XTemplate string conversion defined in section 7.8. Interpolation does not use host-language string coercion.

Examples:

```html
{{ state.count }}
{{ item.label }}
{{ state.first + " " + state.last }}
```

---

# Part II — Expression Model

## 7. XTemplate expression language

XTemplate expressions are a small language owned and defined by XTemplate. The syntax is intentionally JavaScript-like, but the language semantics are
defined by this specification and MUST NOT depend on a JavaScript runtime or the coercion, global scope, object model, or parser rules of a host
language.

Every valid expression in this language version MUST be parseable into an implementation-neutral AST and evaluable without a JavaScript engine. A
conforming JavaScript compiler and a conforming C# evaluator MUST implement the same XTemplate semantics.

Representative expressions are:

```text
state.value
state.value + 1
state.value == 'a' || state.value == 'b'
state.array[state.index].var2 + 3 / 12
state.val1 ? '123' : '232'
state.variable ?? 'default'
state.enabled && !state.disabled
(state.price * state.quantity) + state.tax
item.name
state.items[index].name
```

An expression is read-only and has no side effects. Assignments are not expressions. `x-model` uses the separate assignable-expression subset in
section 41.

### 7.1 Value model

Expression values belong to these language-level kinds:

```text
null
boolean
number
string
object
collection
```

`null` is the sole nullish value. Host-only sentinel values such as JavaScript `undefined` are not XTemplate values. A host adapter MUST map a missing
value to XTemplate `null` or report a context-construction error before evaluation.

Numbers follow the finite IEEE 754 binary64 value set. Numeric literals and arithmetic results MUST be representable as finite binary64 values.
Division or modulo by zero and any operation producing a non-finite value are evaluation errors. Implementations MUST NOT expose `NaN`, positive
infinity, or negative infinity as XTemplate results.

Objects are named-member containers. Collections are ordered, zero-based value sequences. A host may adapt dictionaries, DTOs, arrays, and lists to
these kinds, but host methods, constructors, prototypes, reflection metadata, and indexers not exposed by the adapter are not members of the
XTemplate value.

### 7.2 Lexical rules

Whitespace consists of space, tab, carriage return, and line feed and may occur between tokens. Whitespace is not permitted inside a token.

An identifier matches:

```text
[A-Za-z_][A-Za-z0-9_]*
```

Identifiers are case-sensitive. `null`, `true`, and `false` are reserved literal tokens and cannot be identifiers.

A number is one or more ASCII digits, optionally followed by `.` and one or more ASCII digits:

```ebnf
number = digit, { digit }, [ ".", digit, { digit } ] ;
```

Leading `+` or `-` is a unary operator, not part of a numeric token. Exponent notation, numeric separators, hexadecimal, binary, octal, leading-dot
decimals, and trailing-dot decimals are not supported.

A string is enclosed in single or double quotes. It may contain any Unicode character except an unescaped matching quote, backslash, carriage return,
or line feed. The following escapes are supported:

| Escape | Value |
|---|---|
| `\\` | backslash |
| `\'` | single quote |
| `\"` | double quote |
| `\n` | line feed |
| `\r` | carriage return |
| `\t` | tab |
| `\b` | backspace |
| `\f` | form feed |
| `\uHHHH` | UTF-16 code unit written as exactly four hexadecimal digits |

An escape may be used with either quote style. Unknown, incomplete, or malformed escapes are syntax errors. A raw line break in a string is a syntax
error. Consecutive `\uHHHH` escapes may encode a UTF-16 surrogate pair; an unpaired surrogate is invalid.

The complete operator and punctuation token set is:

```text
.  [  ]  (  )  ?  :
+  -  *  /  %  !
<  <=  >  >=  ==  !=  &&  ||  ??
```

Tokenization uses the longest valid token. Any other character or token, including a single `=`, `&`, or `|`, is invalid. When an expression is read
from an HTML attribute, HTML character-reference decoding occurs before expression tokenization.

The contiguous character sequences `++` and `--` are invalid update-operator tokens; they MUST NOT be interpreted as two unary operators. Nested
unary operators remain expressible when separated, for example `+ +value`.

### 7.3 Grammar

The following grammar is normative. `{ X }` means zero or more repetitions and `[ X ]` means an optional production.

```ebnf
expression       = conditional ;
conditional      = coalesce, [ "?", expression, ":", expression ] ;
coalesce         = logical-or, { "??", logical-or } ;
logical-or       = logical-and, { "||", logical-and } ;
logical-and      = equality, { "&&", equality } ;
equality         = comparison, { ( "==" | "!=" ), comparison } ;
comparison       = additive, { ( "<" | "<=" | ">" | ">=" ), additive } ;
additive         = multiplicative, { ( "+" | "-" ), multiplicative } ;
multiplicative   = unary, { ( "*" | "/" | "%" ), unary } ;
unary            = ( "!" | "+" | "-" ), unary | member ;
member           = primary, { ".", identifier | "[", expression, "]" } ;
primary          = identifier | literal | "(", expression, ")" ;
literal          = "null" | "true" | "false" | number | string ;
```

The grammar permits `??`, `||`, and `&&` to be mixed without additional host-language restrictions. Their precedence is exactly the precedence shown
above. The conditional operator is right-associative because each branch is an `expression`. Repeated binary operators are left-associative.

The parser MUST consume the entire expression. Empty expressions and trailing tokens are syntax errors.

### 7.4 Operator precedence and associativity

From highest to lowest precedence:

| Precedence | Form | Associativity |
|---:|---|---|
| 1 | member `.` and index `[]` access | left |
| 2 | unary `!`, unary `+`, unary `-` | right |
| 3 | `*`, `/`, `%` | left |
| 4 | `+`, `-` | left |
| 5 | `<`, `<=`, `>`, `>=` | left |
| 6 | `==`, `!=` | left |
| 7 | `&&` | left |
| 8 | `||` | left |
| 9 | `??` | left |
| 10 | `?:` | right |

Parentheses override this table.

### 7.5 Evaluation context and identifier resolution

Evaluation receives an explicit `ExpressionContext`, conceptually:

```text
ExpressionContext
    identifiers:
        state
        item
        index
        indexAbsolute
        indent
        ...
```

The context is a case-sensitive mapping from identifier names to XTemplate values. An identifier MUST be resolved only from that mapping. An unknown
identifier is an evaluation error and SHOULD also be a validation error when the available context is statically known.

There is no fallback to host globals. In particular, `window`, `document`, `globalThis`, `location`, `fetch`, `Math`, `Date`, `Function`, and `eval`
do not automatically exist. A future language version may define controlled XTemplate built-ins; such built-ins would be language features with
specified portable semantics, not access to host-language functions.

The available identifiers depend on template location:

- ordinary expressions normally receive `state`;
- content inside `x-for` additionally receives its declared item variable and its declared or implicit `index` variable;
- content inside `x-recursive` additionally receives its declared variables, `index`, `indexAbsolute`, and `indent` as specified by the recursive
  construct;
- an enclosing lexical context remains visible unless a loop declaration shadows the same name.

Runtime/compiler services such as `handler`, `invalidate`, or VDOM utilities are not expression identifiers merely because a JavaScript render
function receives them. An implementation may expose additional application values only by explicitly placing them in the expression context and
documenting that directive context.

Language syntax, evaluation context, and directive-specific identifiers are separate contracts. Parsing determines whether text is a valid
expression; context validation determines whether its identifiers are available at that template location.

### 7.6 Member and indexed access

`value.name` reads the named member `name`. For objects, the member must be explicitly exposed by the context adapter. Collections and strings expose
the single built-in member `length`; for collections it is the element count, and for strings it is the number of UTF-16 code units. No other
collection or string members or methods are implicit.

`value[index]` evaluates `index` and then reads:

- an object member when the index is a string;
- a collection element when the index is a non-negative integer number.

Member names are case-sensitive. A missing object member, an out-of-range collection index, or access through a `null` base evaluates to `null`. The
remainder of a member/index chain also evaluates to `null` without a host exception. Therefore:

```text
state.user.name ?? 'Anonymous'
```

evaluates to `'Anonymous'` when `state.user` or `state.user.name` is missing or `null`.

Access with an unsupported base or index kind is an evaluation error. Implementations MUST NOT consult JavaScript prototypes, invoke .NET reflection
members implicitly, or call property getters that were not explicitly exposed by the host adapter.

### 7.7 Truthiness and control flow

XTemplate truthiness is:

| Value | Truthiness |
|---|---|
| `null` | false |
| `false` | false |
| `true` | true |
| number `0` or `-0` | false |
| any other number | true |
| empty string `''` or `""` | false |
| non-empty string | true |
| object | true |
| collection, including an empty collection | true |

`!value` returns the boolean negation of this truthiness.

`left && right` evaluates `left`; if it is falsy, the operator returns `left` without evaluating `right`, otherwise it evaluates and returns `right`.

`left || right` evaluates `left`; if it is truthy, the operator returns `left` without evaluating `right`, otherwise it evaluates and returns `right`.

`value ?? fallback` evaluates `value`; if it is not `null`, the operator returns it without evaluating `fallback`, otherwise it evaluates and returns
`fallback`. No other value is nullish.

`condition ? whenTrue : whenFalse` evaluates the condition and exactly one branch, selected by XTemplate truthiness.

### 7.8 Arithmetic and string conversion

Unary `+` and unary `-`, and binary `-`, `*`, `/`, and `%`, require numeric operands. There is no implicit conversion from strings, booleans, or
`null` to numbers.

`/` performs binary64 division. Division by zero is an evaluation error. `%` is the remainder after a quotient truncated toward zero; the result has
the sign of the left operand. Modulo by zero is an evaluation error.

Binary `+` performs numeric addition when both operands are numbers. If either operand is a string, it performs concatenation after converting the
other operand with this scalar string conversion:

| Value | String form |
|---|---|
| `null` | empty string |
| boolean | `true` or `false` |
| number | shortest invariant-culture decimal text that round-trips to the same binary64 value; `-0` is `0` |
| string | unchanged |
| object or collection | evaluation error |

If neither operand is a number pair and neither is a string, `+` is an evaluation error. Interpolation and text directives use the same scalar string
conversion; a renderer MAY define directive-specific handling for object or collection content only where that directive explicitly requires such
values.

### 7.9 Equality and comparison

`==` is strict XTemplate equality and does not coerce types:

- values of different kinds are not equal;
- `null` equals `null`;
- booleans compare by boolean value;
- numbers compare by binary64 numeric value, with `0` equal to `-0`;
- strings compare by ordinal Unicode scalar sequence;
- objects and collections compare by identity within the supplied evaluation context, not by deep contents.

`!=` is the boolean negation of `==`. Consequently:

```text
1 == 1       → true
1 == '1'     → false
null == null → true
true == true → true
```

`===` and `!==` are not operators in this language version. `==` and `!=` already provide the non-coercive equality model.

`<`, `<=`, `>`, and `>=` accept either two numbers or two strings. Numbers use numeric ordering. Strings use ordinal Unicode scalar ordering. Other
or mixed operand kinds are evaluation errors; there is no implicit conversion.

### 7.10 Unsupported constructs

Arbitrary JavaScript is not valid XTemplate expression syntax. The initial language has no calls, methods, assignments, updates, statements, object
or array literals, or host-language escape hatch. The following are invalid:

```text
foo()
state.foo()
Math.round(value)
new Date()
new Something()
() => value
function() {}
state.value = 3
state.value += 2
state.value++
++state.value
delete state.value
await value
yield value
class {}
import('module')
eval('code')
state.name.toUpperCase()
state.items.filter(x => x.enabled)
{ name: 'test' }
[1, 2, 3]
```

Specifically, this version does not support function or method calls, function or class declarations, arrow functions, constructors, `new`,
assignments of any kind, increment/decrement, `await`, `yield`, `delete`, `typeof`, `instanceof`, `in`, comma/sequence expressions, template literals,
optional chaining, object literals, or array literals.

Complex computation belongs in component logic or state. For example, replace historical template code such as:

```text
state.items.filter(x => x.enabled).length
```

with a state value such as:

```text
state.enabledItemCount
```

### 7.11 Expression AST

An expression parser MUST represent structure rather than retaining an unvalidated source string as executable code. The implementation-neutral
model contains at least these node kinds:

```text
Literal(value)
Identifier(name)
MemberAccess(target, memberName)
IndexAccess(target, index)
UnaryExpression(operator, operand)
BinaryExpression(operator, left, right)
ConditionalExpression(condition, whenTrue, whenFalse)
```

Nodes SHOULD retain source spans for diagnostics. Exact class names and storage layout are implementation details.

For example, `state.array[state.index].var2 + 3 / 12` parses conceptually as:

```text
BinaryExpression(+)
├── MemberAccess(var2)
│   └── IndexAccess
│       ├── MemberAccess(array)
│       │   └── Identifier(state)
│       └── MemberAccess(index)
│           └── Identifier(state)
└── BinaryExpression(/)
    ├── Literal(3)
    └── Literal(12)
```

### 7.12 Compiler and evaluator requirements

A JavaScript backend MUST use this pipeline:

```text
source expression
→ tokenize
→ parse
→ validate
→ expression AST
→ emit JavaScript
```

Generated JavaScript may resemble the input, but only after validation. The emitter MUST preserve XTemplate null propagation, equality, truthiness,
numeric, and access semantics; emitting a host operator directly is conforming only when it is observably equivalent for all permitted operands.

A C# or other direct renderer evaluates the same AST. Every valid XTemplate expression in this language version MUST be evaluable without Node.js,
a browser, `eval`, `new Function`, a JavaScript interpreter, or arbitrary JavaScript execution.

### 7.13 Expression diagnostics

Syntax, validation, and evaluation errors are distinct. Diagnostics SHOULD contain a source offset or span and enough context to identify the
expression and directive. Implementations need not produce byte-identical wording, but MUST accept and reject the same constructs.

Representative diagnostics include:

```text
Unexpected token '(' after identifier 'foo': function calls are not supported.
Assignment operator '=' is not valid in XTemplate expressions.
Unknown identifier 'window'.
Expected expression after '+'.
Missing closing ']'.
Unexpected token 'new': constructors are not supported.
```

### 7.14 Directive integration

The following constructs consume the restricted expression grammar:

| Construct | Expression role | Additional context or rule |
|---|---|---|
| `{{ ... }}` | value | converted to text |
| `x-text`, `x-html`, `x-children` | value | directive-specific output handling |
| `x-attr`, `x-attr:name` | value | whole-object form expects an object |
| `x-attr:[expression]` | dynamic name | bracket contents use this grammar |
| `x-prop`, `x-prop:name` | value | subject to the support rules in Part IV |
| `x-prop:[expression]` | dynamic name | bracket contents use this grammar |
| `x-if`, `x-elseif`, `x-show`, `x-class:name` | condition | XTemplate truthiness |
| `x-for` collection | value | evaluated in the enclosing context before loop locals exist |
| `x-recursive` collection | value | evaluated in the enclosing context; nested content receives recursive locals |
| `x-model` | assignable expression | restricted further by section 41 |

`x-on:event` does not consume an expression; its value is a command name/string. `x-key` is a property name under the current XTemplate contract, not
an arbitrary expression. Implementations MUST preserve these distinctions.

### 7.15 Historical compatibility

Historically, the browser compiler inserted expression text into generated JavaScript. That implementation detail allowed arbitrary JavaScript and
is not the language contract defined by this version. Unsupported JavaScript syntax MUST NOT be accepted merely for compatibility or implementation
convenience.

Migration consists of moving calls, transformations, and other complex computation into component logic/state and exposing their results as simple
context values. This restriction is intentionally breaking for templates that relied on executable JavaScript.

The repository at the time of this language change contains historical templates that use constructs such as `Math.floor(...)`, string methods
(`endsWith`, `startsWith`, `indexOf`, `split`, `join`), and `i18n` method calls. Those templates require migration to precomputed state/context values
before they conform to this specification. A historical `x-model="state.roles.join(', ')"` is additionally invalid because a call is not an assignable
target. No current-language support for calls is implied by those legacy examples. The audit found no requirement for object or array literal syntax,
so those literals remain unsupported.

### 7.16 Expression conformance

A conforming expression implementation MUST:

- parse every syntactically valid expression generated by the grammar, then apply the specified validation and evaluation errors, and reject
  unsupported JavaScript constructs;
- consume the complete input and preserve the precedence and associativity table;
- implement short-circuit evaluation and evaluate only a selected conditional branch;
- implement null/member/index access, equality, truthiness, string conversion, and numeric behavior exactly as specified;
- resolve identifiers only from the explicit evaluation context;
- distinguish ordinary expressions from assignable expressions;
- produce equivalent observable values or equivalent errors across JavaScript and C# implementations.

Representative valid cases are the examples at the start of section 7. Representative invalid categories are calls (`state.getValue()`), host globals
(`Math.round(state.value)`, `window.location`), constructors (`new Date()`), assignments and updates (`state.value = 10`, `state.value++`), functions
(`() => 1`), async/module syntax (`await state.value`, `import('module')`), and collection/object literals (`[1, 2, 3]`, `{ value: 1 }`).

---

# Part III — Nodes and Content Directives

## 8. Static elements and attributes

Ordinary HTML is emitted normally:

```html
<section class="card" aria-label="Example">
    Hello
</section>
```

Static attributes are literal strings unless they are boolean HTML attributes interpreted by the browser.

XTL does not evaluate ordinary attribute text as an expression.

Dynamic values require an XTL binding directive.

---

## 9. `x-text`

Syntax:

```html
<element x-text="expression"></element>
```

Example:

```html
<span x-text="state.label"></span>
```

Semantics:

1. evaluate the expression;
2. convert it with the XTemplate scalar string conversion in section 7.8;
3. set the element's text content.

The element MUST NOT contain authored child nodes.

A compiler SHOULD report a diagnostic when `x-text` is used on a non-empty element.

Object and collection values produce an evaluation error because the scalar string conversion does not define a representation for them.

---

## 10. `x-html`

Syntax:

```html
<element x-html="expression"></element>
```

Example:

```html
<div x-html="state.html"></div>
```

Semantics:

1. evaluate the expression;
2. convert it with the XTemplate scalar string conversion in section 7.8;
3. assign it as raw HTML content.

Object and collection values produce an evaluation error because the scalar string conversion does not define a representation for them.

The element MUST NOT contain authored child nodes.

### Security

`x-html` performs raw HTML insertion.

XTL does not sanitize the value.

Applications are responsible for ensuring the value is trusted or sanitized.

---

## 11. `x-children`

Syntax:

```html
<element x-children="expression"></element>
```

Example:

```html
<span x-children="state.svg"></span>
```

The expression represents real DOM node content rather than text or XTL VDOM children.

The value may be:

- a DOM Node;
- an array of DOM Nodes;
- `null`/empty.

Semantics:

```text
array   → replace children and append each node
node    → make that node the element child
empty   → remove existing children
```

A conforming template SHOULD NOT combine `x-children` with authored child nodes.

---

# Part IV — Attribute and Property Binding

## 12. Static attributes versus properties

XTL distinguishes:

- **attributes** — serialized DOM attributes;
- **properties** — JavaScript properties on the DOM/custom element instance.

Use `x-attr:*` for attributes.

Use `x-prop:*` for properties.

This distinction is important for custom elements, complex objects, arrays, DOM objects, and values that cannot be represented faithfully as strings.

---

## 13. `x-attr:name`

Syntax:

```html
<element x-attr:name="expression"></element>
```

Example:

```html
<a x-attr:href="state.url"></a>
```

Semantics:

```text
expression result → attribute named "name"
```

Current DOM behavior:

- boolean `true` → present empty attribute;
- boolean `false` → attribute removed;
- `null` → attribute removed;
- primitive value → assigned via `setAttribute`;
- object value → current renderer treats truthy object keys as a space-separated attribute value.

Example:

```html
<button x-attr:disabled="state.disabled"></button>
```

---

## 15. `x-attr` object expansion

Syntax:

```html
<element x-attr="expression"></element>
```

The expression is converted to an attribute object.

Example:

```html
<div x-attr="state.attributes"></div>
```

The current `utils.toObject` behavior copies only values whose types are:

- string;
- number;
- boolean `true`.

Boolean `false` is omitted.

Other value types are omitted.

A compatible implementation targeting current behavior SHOULD preserve these conversion rules.

---

## 16. Dynamic attribute names

The current compiler supports bracketed dynamic names:

```html
<div x-attr:[expression]="valueExpression"></div>
```

The bracket contents and the value are independently parsed as restricted XTemplate expressions. The name expression is evaluated first and
converted to a dynamic attribute name by the renderer's attribute-name rules; it is never executed as JavaScript.

This form exists in the compiler but has little evidence in current templates.

It should therefore be considered **supported but low-confidence / advanced syntax** until dedicated conformance tests exist.

---

## 17. `x-prop:name`

Syntax:

```html
<element x-prop:name="expression"></element>
```

Example:

```html
<x-datafield x-prop:domain="state.domain"></x-datafield>
```

Semantics:

1. evaluate the expression;
2. assign the result to the element property.

Property names are converted from kebab-case to camelCase.

Example:

```html
<div x-prop:some-value="state.value"></div>
```

targets:

```js
element.someValue
```

### 17.1 Deferred custom-element properties

The current renderer supports elements whose custom-element class is not fully initialized when VDOM properties are applied.

When the target property does not yet exist, the runtime may defer the assignment until the custom element initializes.

This is a runtime behavior, not syntax, but compatible XShell renderers should preserve it.

---

## 19. Dynamic property names

The compiler contains support for:

```html
<element x-prop:[expression]="valueExpression"></element>
```

The bracket contents and the value are independently parsed as restricted XTemplate expressions. The name expression is evaluated first and
converted to a dynamic property name by the renderer's property-name rules; it is never executed as JavaScript.

This syntax is implemented but not broadly evidenced in application templates and should be treated as advanced syntax pending focused tests.

---

## 20. Whole-object `x-prop`

The current reference parser recognizes:

```html
<element x-prop="expression"></element>
```

The apparent intended meaning is property-object expansion.

However, the current reference compiler places this expansion into the **attribute object**, not the property object.

Because implementation and naming disagree, whole-object `x-prop` is **not supported XTemplate syntax** and is not part of the normative language.

Implementers SHOULD support `x-prop:name`, which is well-defined.

A future specification revision should decide whether whole-object `x-prop` means property expansion and fix the reference implementation accordingly.

---

# Part V — CSS Class and Visibility

## 21. `x-class:name`

Syntax:

```html
<element x-class:class-name="expression"></element>
```

Example:

```html
<li x-class:selected="state.selected"></li>
```

If the expression is truthy, the class is present.

If it is falsy, the class is absent.

Static classes and dynamic classes are merged.

Example:

```html
<a class="menuitem plain"
   x-class:selected="state.selected">
</a>
```

produces a class list containing:

```text
menuitem plain
```

plus `selected` when the expression is truthy.

Multiple `x-class:*` bindings may appear on one element.

---

## 22. `x-show`

Syntax:

```html
<element x-show="expression"></element>
```

`x-show` does not structurally remove the element.

When the expression is falsy, the current implementation adds an inline attribute equivalent to:

```html
style="display:none"
```

When truthy, that hiding style is absent.

This differs fundamentally from `x-if`.

### 22.1 Combination with other style bindings

The current implementation realizes `x-show` as an attribute-object contribution.

Combining it with another independently generated `style` binding may be order-sensitive in the current compiler.

Templates SHOULD avoid ambiguous combinations until explicit style-merging semantics are standardized.

---

# Part VI — Events

## 23. Event binding

Canonical syntax:

```html
<element x-on:event="command"></element>
```

Example:

```html
<button x-on:click="save">Save</button>
```

The right-hand side is a **command name**, not inline JavaScript.

On the event, XTL conceptually calls:

```js
handler("save", event)
```

The component/runtime decides how the command is dispatched.

This command-oriented event model is a defining XTL characteristic.

---

## 25. Event modifiers

Modifiers are appended to the event name:

```html
<button x-on:click.stop="save"></button>
<input x-on:keydown.enter="submit">
```

The current runtime recognizes behavior for the following modifier categories.

### 25.1 Flow modifiers

```text
.stop
.prevent
```

Semantics:

```text
.stop     → event.stopPropagation()
.prevent  → event.preventDefault()
```

These actions occur after invocation of the bound event handler in the current runtime.

### 25.2 Mouse-button filters

```text
.left
.middle
.right
```

Intended semantics are to invoke only for the selected mouse button.

The reference implementation currently contains suspicious boolean expressions for these checks. Implementers SHOULD follow the intended filter semantics rather than reproducing JavaScript operator-precedence bugs.

### 25.3 Modifier-key filters

```text
.alt
.shift
.ctrl
```

The event is handled only when the corresponding modifier key is active.

The reference implementation contains a likely typo for the Alt-key property (`altlKey` instead of `altKey`). This is an implementation defect, not a language rule.

### 25.4 Keyboard filters

For keyboard events:

```text
.escape
.enter
.tab
.backspace
.delete
.space
.up
.down
.left
.right
```

Expected key mappings:

| Modifier | `event.key` |
|---|---|
| `escape` | `Escape` |
| `enter` | `Enter` |
| `tab` | `Tab` |
| `backspace` | `Backspace` |
| `delete` | `Delete` |
| `space` | `" "` |
| `up` | `ArrowUp` |
| `down` | `ArrowDown` |
| `left` | `ArrowLeft` |
| `right` | `ArrowRight` |

### 25.5 Unknown modifiers

The current runtime parses all dot-separated modifiers into an options object.

Only modifiers with defined XTL semantics should be relied on.

An implementation MAY reject unknown modifiers.

---

# Part VII — Conditional Rendering

## 26. `x-if`

Syntax:

```html
<element x-if="expression">...</element>
```

When the expression is truthy, the element is rendered.

When falsy, the element is structurally absent.

In the current XShell VDOM backend, a false branch is represented by a comment placeholder occupying the same logical position.

This preserves stable sibling indexes during reconciliation.

---

## 27. `x-elseif`

Syntax:

```html
<element x-elseif="expression">...</element>
```

`x-elseif` belongs to the immediately preceding conditional chain at the same parent level.

It is rendered only when:

1. no earlier branch in the chain matched; and
2. its own expression is truthy.

---

## 28. `x-else`

Syntax:

```html
<element x-else>...</element>
```

It is rendered when no earlier branch in the same chain matched.

---

## 29. Conditional chain

Canonical form:

```html
<div x-if="state.kind === 'a'">A</div>
<div x-elseif="state.kind === 'b'">B</div>
<div x-else>Other</div>
```

A conforming template MUST treat these branches as one contiguous sibling chain.

`x-elseif` and `x-else` SHOULD immediately follow a previous branch in the same chain, ignoring only insignificant authoring conventions specifically allowed by a validator.

### 29.1 Reference-compiler detail

The current compiler stores condition state by template nesting depth.

It does not robustly validate adjacency and can therefore associate a later `x-else` with the most recent `x-if` at the same level.

This is an implementation shortcut and SHOULD NOT be treated as permission to write non-contiguous chains.

---

# Part VIII — Repetition

## 30. `x-for`

Basic syntax:

```html
<element x-for="item in expression">...</element>
```

Example:

```html
<li x-for="item in state.items">
    {{ item.label }}
</li>
```

The collection expression is evaluated once for the list render operation and normalized with XTL collection semantics.

The element is instantiated once per normalized item.

---

## 31. `x-for` with index

Tuple syntax:

```html
<element x-for="(item,index) in expression">...</element>
```

Example:

```html
<li x-for="(item,index) in state.items">
    {{ index }}: {{ item.label }}
</li>
```

The second variable receives a zero-based iteration index.

If the tuple form is not used, the implicit index variable is named:

```text
index
```

---

## 32. Collection normalization

An `x-for` or `x-recursive` source is normalized with the following XTemplate rules. These rules describe language behavior; a renderer MUST NOT
depend on a JavaScript `.map` method or other host collection API.

### Array

```text
[ a, b, c ] → [ a, b, c ]
```

### Number

A number `N` becomes:

```text
1, 2, ..., N
```

Example:

```html
<span x-for="n in 3">{{ n }}</span>
```

iterates over:

```text
1, 2, 3
```

### String

A string becomes a collection of Unicode code points in source order. A supplementary character represented by a UTF-16 surrogate pair is one item.

Example:

```text
"abc" → ["a", "b", "c"]
```

### Object

A non-null object is converted to a collection of the member-name strings exposed by its context adapter. Iteration yields **keys**, not member
values. The adapter MUST provide deterministic key order; JavaScript adapters use own enumerable string-key order for compatibility with historical
`Object.keys` behavior.

### Other values

`null` and booleans are not iterable and produce an evaluation error. A finite non-negative integer is required for number iteration; negative,
fractional, and non-finite numbers produce an evaluation error.

---

## 33. Positional list identity

Without `x-key`, list identity is positional.

Example:

```html
<li x-for="item in state.items">
    {{ item.label }}
</li>
```

The current VDOM renderer marks the list region as:

```text
forType = "position"
```

and reconciles list children by position.

---

## 34. `x-key`

Syntax:

```html
<element x-for="item in expression" x-key="propertyName">
```

Example:

```html
<li x-for="item in state.items" x-key="id">
    {{ item.label }}
</li>
```

In the current language, `x-key` is a **property name**, not an arbitrary expression.

The effective key is:

```js
item[propertyName]
```

The current compiler emits property access equivalent to:

```js
item.id
```

for:

```html
x-key="id"
```

Implementations SHOULD require a valid property-name form unless the language is explicitly extended.

Duplicate keys are invalid for reliable keyed reconciliation.

The current runtime warns when duplicate keys are detected.

---

## 35. List region boundaries

The current XShell VDOM ABI wraps each `x-for` result with logical markers:

```text
#comment "x-for-start"
rendered items...
#comment "x-for-end"
```

The start and end nodes carry:

```text
forType: "position"
```

or:

```text
forType: "key"
```

This is **runtime ABI**, not mandatory language syntax.

A non-VDOM implementation may use another range representation as long as observable list behavior is equivalent.

---

# Part IX — Recursive Tree Rendering

## 36. `x-recursive`

`x-recursive` is a specialized recursive-list construct.

Basic syntax:

```html
<element x-recursive="item in expression">
    ...
</element>
```

Example:

```html
<x-menuitem
    x-recursive="menuitem in state.menu"
    x-key="href"
    x-attr:label="menuitem.label">
</x-menuitem>
```

It renders the initial collection and then recursively renders:

```js
item.children
```

for each item.

The child-property name is currently fixed to:

```text
children
```

---

## 37. Recursive tuple variables

Supported form:

```html
<element x-recursive="(item,index,indexAbsolute) in expression">
```

Variables:

```text
item
index
indexAbsolute
indent
```

Semantics:

- `item` — current item;
- `index` — zero-based index within the current sibling collection;
- `indexAbsolute` — running index across recursive traversal;
- `indent` — recursion depth, starting at `0`.

If tuple names are omitted:

```text
index
indexAbsolute
indent
```

are the reference names used by the compiler.

The third tuple element may rename `indexAbsolute`.

`indent` is currently implicit and is not renamed by tuple syntax.

---

## 38. Recursive children

After rendering each current item, recursive rendering conceptually performs:

```js
renderRecursive(
    item.children,
    indexAbsolute + 1,
    indent + 1
)
```

`null` or empty children produce no recursive item content.

---

## 39. `x-recursive-wrapper`

Syntax:

```html
<element
    x-recursive="item in state.items"
    x-recursive-wrapper="tag-name">
```

Example:

```html
<li
    x-recursive="menuitem in state.menu"
    x-recursive-wrapper="ul">
```

The wrapper applies around the rendered recursive children at deeper levels.

Conceptually:

```html
<li>current item content
    <ul>
        recursive child items...
    </ul>
</li>
```

The wrapper value is a tag name in the current implementation.

---

## 40. `x-key` with recursion

`x-key` may be used with `x-recursive`.

Its semantics are equivalent to keyed `x-for` identity for each recursive list region.

---

# Part X — Two-Way Model Binding

## 41. `x-model`

Syntax:

```html
<element x-model="assignableExpression"></element>
```

Example:

```html
<input x-model="state.name">
```

`x-model` combines:

1. model → element property binding;
2. element change → model assignment;
3. invalidation after assignment.

Conceptually:

```text
render:
    element.value = model

change:
    model = normalizedInputValue(element)
    invalidate()
```

The value MUST be an `AssignableExpression`, which is a strict subset of `Expression`. Its grammar is:

```ebnf
assignable-expression = identifier, { ".", identifier | "[", expression, "]" } ;
```

At least one member or index suffix is REQUIRED; replacing a root context binding such as `state` or `item` is not permitted. Each index is an
ordinary, read-only XTemplate expression. Calls, operators, conditionals, coalescing, and literals cannot form the outer assignment target.

Recommended forms include:

```text
state.name
state.user.name
item.value
state.items[index].name
```

The assignment algorithm resolves the root identifier from the explicit evaluation context, evaluates each index exactly once from left to right,
and traverses all suffixes except the final suffix as reads. Every intermediate container MUST exist and be non-null. For the final suffix:

- an object member is assigned when it is writable; the member may be created only when the context adapter explicitly permits member creation;
- a collection index is assigned only when it is an in-range, non-negative integer and the collection is writable.

A missing intermediate, null, unsupported, out-of-range, or read-only target produces a model-assignment error. Write traversal does not use the
null-propagating read behavior to silently discard an assignment.

These are not assignable expressions and MUST be rejected during template validation:

```text
state.a + state.b
state.value ?? 'default'
state.enabled ? state.a : state.b
```

An assignable expression is read with the normal expression semantics during rendering and is used as a validated location during write-back.

---

## 42. Model update event

The current implementation uses:

```text
change
```

for model updates.

It does not use the `input` event for text fields.

The generated event semantics include propagation stopping, equivalent to:

```text
change.stop
```

---

## 43. Input value normalization

The reference helper behaves as follows.

### Normal input / custom element

```text
target.value
```

### `<input type="number">`

```text
target.valueAsNumber
```

### `<input type="range">`

```text
target.valueAsNumber
```

### `<input type="checkbox">`

```text
target.checked
```

### `<input type="radio">`

write-back value:

```text
target.value
```

### `<select>`

The current helper reads all selected options and returns their values joined with commas:

```js
Array.from(target.selectedOptions)
    .map(option => option.value)
    .join(",")
```

This behavior is unusual and should be preserved only when compatibility with the existing runtime is required.

---

## 44. Model render property

The reference compiler chooses the render-side property approximately as follows:

| Element | Type | Property |
|---|---|---|
| generic/custom | — | `value` |
| `input` | normal | `value` |
| `input` | `range` | `valueAsNumber` |
| `input` | `checkbox` | `checked` |
| `input` | `radio` | `checked` |
| `select` | normal | `value` |
| `select` | `multiple` | current implementation attempts special handling |

### 44.1 Radio implementation defect

The current compiler's radio checked-expression is effectively hardcoded around `state.value` rather than consistently using the actual `x-model` expression.

This should be considered a reference implementation defect.

Intended semantics are:

```text
radio.checked = (modelValue == radio.value)
```

### 44.2 Multiple-select ambiguity

The current compilation path for `select[multiple]` references `event` while building a render-side property expression, which is not well-defined during ordinary render evaluation.

This is not sufficiently reliable to establish normative language semantics.

A compatible implementation should treat multi-select `x-model` as an area requiring explicit conformance decisions/tests rather than blindly copying the current generated expression.

---

# Part XI — Render Controls

## 45. `x-once`

Syntax:

```html
<element x-once>...</element>
```

The element is fully rendered on the first render.

On subsequent renders, its existing DOM subtree is preserved and is not diffed.

The current VDOM compiler emits a special VNode with:

```text
options.once = true
```

after the initial render.

This means `x-once` establishes a render boundary whose DOM content is intentionally stale relative to future state changes.

---

## 46. `x-pre`

Syntax:

```html
<element x-pre>
    ...
</element>
```

`x-pre` disables XTL compilation of the element's child markup.

Its inner content is treated as literal HTML.

Example:

```html
<pre x-pre>
    {{ this.is.not.an.expression }}
</pre>
```

The interpolation markers inside the literal content remain literal text/HTML rather than executing as XTL expressions.

The current compiler restores synthetic `x:text` markers back into `{{` and `}}` when serializing the literal inner HTML.

An implementation SHOULD treat the entire child subtree of `x-pre` as opaque to XTL directives/interpolation.

---

# Part XII — Custom Elements and Dependencies

## 47. Custom-element dependency discovery

The current X template layer scans parsed template elements for tag names containing a hyphen:

```text
x-button
x-datafield
my-component
```

Such tags are considered component dependencies.

Duplicates are removed.

The dependency name is normalized to lowercase.

---

## 48. `x-lazy` dependency boundary

`x-lazy` has special dependency-discovery semantics.

The `x-lazy` element itself is a dependency.

Custom elements nested inside `x-lazy` are not eagerly reported as template dependencies.

Conceptually:

```html
<x-lazy>
    <x-heavy-component></x-heavy-component>
</x-lazy>
```

eager dependencies include:

```text
x-lazy
```

but not necessarily:

```text
x-heavy-component
```

This rule affects dependency loading, not rendering syntax.

A compiler that also emits dependency metadata SHOULD preserve this behavior.

---

# Part XIII — Abstract Rendering Semantics

## 49. Abstract rendered node

A useful target-neutral representation of an XTL rendered node is:

```text
RenderedNode {
    tag
    attributes
    properties
    events
    options
    children
}
```

Where:

```text
tag         = HTML/custom tag, #text, or #comment
attributes  = dynamic/static DOM attributes
properties  = DOM/custom-element properties
events      = event bindings
options     = renderer metadata
children    = rendered child nodes, text, raw HTML, or raw DOM nodes
```

An interpreter may apply this directly to DOM.

A compiler may emit code that constructs this structure.

A server renderer may instead translate the same semantics to HTML where possible.

---

# Part XIV — Current XShell VDOM ABI

## 50. VNode constructor

The current XShell runtime uses the conceptual signature:

```js
utils.createVDOM(
    tag,
    attrs,
    props,
    events,
    options,
    children,
    moreChildren
)
```

The resulting object is:

```js
{
    tag,
    attrs,
    props,
    events,
    options,
    children
}
```

Null attribute/property/event objects are normalized to empty objects.

---

## 51. `options.index`

Every ordinary generated VNode has a logical sibling index.

Example:

```js
{
    index: 0
}
```

The current reconciler uses this index to preserve stable DOM positions.

False conditional branches also use placeholders with the same logical index.

A compiler targeting the existing renderer MUST reproduce compatible index semantics.

---

## 52. Text node

Conceptual VNode:

```js
utils.createVDOM(
    "#text",
    null,
    null,
    null,
    { index: N },
    text
)
```

---

## 53. Comment node

Conceptual VNode:

```js
utils.createVDOM(
    "#comment",
    null,
    null,
    null,
    { index: N },
    text
)
```

Comments are used both for authored HTML comments and structural placeholders/range markers.

---

## 54. HTML content

`x-html` results in:

```text
options.format = "html"
```

and string children.

The DOM renderer uses:

```js
element.innerHTML = children
```

---

## 55. DOM-node content

`x-children` results in:

```text
options.format = "node"
```

and node/node-array children.

---

## 56. One-time node

A subsequent-render `x-once` node uses:

```text
options.once = true
```

The DOM diff skips updates for such a node.

---

## 57. Loop markers

Current list regions use comment nodes with:

```text
children = "x-for-start"
children = "x-for-end"
```

and:

```text
options.forType = "position"
```

or:

```text
options.forType = "key"
```

Keyed child nodes additionally carry:

```text
options.key
```

---

# Part XV — Parsing and Compilation Rules

## 58. Recommended parser pipeline

A new compiler SHOULD use the following conceptual phases:

```text
1. Receive template source.
2. Trim outer source if compatibility with current XTL is required.
3. Recognize interpolation.
4. Parse as an HTML fragment.
5. Build a template AST.
6. Tokenize and parse every expression into an expression AST.
7. Validate expression contexts, assignability, and structural/directive rules.
8. Discover custom-element dependencies.
9. Evaluate expression ASTs or emit target code from them.
10. Execute or emit the target artifact.
```

A compiler does not need to literally create `<x:text>` nodes.

That is a reference implementation technique.

A better parser may tokenize interpolation directly as long as the resulting semantics are equivalent.

---

## 59. Recommended template AST

A language-neutral compiler can use an AST similar to:

```text
Template
  children: TemplateNode[]

TemplateNode
  TextNode
  CommentNode
  InterpolationNode
  ElementNode

ElementNode
  tagName
  staticAttributes
  bindings
  directives
  children

ExpressionNode
  Literal
  Identifier
  MemberAccess
  IndexAccess
  UnaryExpression
  BinaryExpression
  ConditionalExpression
```

Suggested binding nodes:

```text
AttributeBinding
PropertyBinding
ClassBinding
EventBinding
ModelBinding
```

Suggested structural directive nodes:

```text
IfBlock
ForBlock
RecursiveBlock
OnceBlock
PreBlock
```

Normalizing structural directives into explicit AST blocks is preferable to reproducing source-order code-generation tricks from the current JavaScript compiler.

---

## 60. Structural directive validation

A compiler SHOULD validate structural constructs before code generation.

Recommended rules:

- `x-elseif` requires a preceding `x-if` / `x-elseif` chain;
- `x-else` requires a preceding chain and terminates that chain;
- `x-key` requires `x-for` or `x-recursive`;
- `x-recursive-wrapper` requires `x-recursive`;
- `x-text`, `x-html`, and `x-children` should not contain authored children;
- `x-model` requires an assignable target for two-way binding;
- unknown `x-*` directives are errors;
- unknown `x:*` pseudo-elements are errors.

The current compiler often logs errors rather than throwing. A new compiler SHOULD produce proper diagnostics.

---

# Part XVI — Informal Grammar

## 61. Directive grammar

This grammar is intentionally descriptive rather than a complete HTML grammar.

```ebnf
template          = html-fragment ;

interpolation     = "{{", expression, "}}" ;

text-directive    = "x-text", "=", quoted-expression ;
html-directive    = "x-html", "=", quoted-expression ;
children-directive= "x-children", "=", quoted-expression ;

attr-binding      = "x-attr:", attr-name, "=", quoted-expression ;
dynamic-attr-binding
                  = "x-attr:[", expression, "]", "=", quoted-expression ;
attr-spread       = "x-attr", "=", quoted-expression ;

prop-binding      = "x-prop:", prop-name, "=", quoted-expression ;
dynamic-prop-binding
                  = "x-prop:[", expression, "]", "=", quoted-expression ;

event-binding     = "x-on:", event-spec, "=", quoted-command ;

class-binding     = "x-class:", class-name, "=", quoted-expression ;

if-directive      = "x-if", "=", quoted-expression ;
elseif-directive  = "x-elseif", "=", quoted-expression ;
else-directive    = "x-else" ;

show-directive    = "x-show", "=", quoted-expression ;

for-directive     = "x-for", "=", quoted-for-expression ;
key-directive     = "x-key", "=", quoted-property-name ;

recursive-directive
                  = "x-recursive", "=", quoted-recursive-expression ;

recursive-wrapper = "x-recursive-wrapper", "=", quoted-tag-name ;

model-directive   = "x-model", "=", quoted-assignable-expression ;

once-directive    = "x-once" ;
pre-directive     = "x-pre" ;
```

### 61.1 Unsupported binding syntax

XTemplate shorthand bindings are not part of the language. The following are invalid XTemplate binding syntax:

```text
:title="state.title"
.value="state.value"
@click="save"
:="state.attributes"
.="state.properties"
```

A conforming parser, compiler, renderer, validator, or tool MUST NOT interpret these forms as XTemplate bindings. Where HTML parsing permits them, they may remain ordinary HTML attributes without XTemplate directive semantics.

Older implementations may have recognized these forms; that historical behavior is not part of the current language.

---

## 62. Loop grammar

```ebnf
for-expression =
      identifier, " in ", expression
    | "(", identifier, ",", identifier, ")", " in ", expression
    ;
```

Examples:

```text
item in state.items
(item,index) in state.items
```

---

## 63. Recursive grammar

```ebnf
recursive-expression =
      identifier, " in ", expression
    | "(", identifier, ",", identifier, ")", " in ", expression
    | "(", identifier, ",", identifier, ",", identifier, ")", " in ", expression
    ;
```

Examples:

```text
item in state.items
(item,index) in state.items
(item,index,indexAbsolute) in state.items
```

The recursive child property is currently fixed to:

```text
children
```

---

## 64. Event grammar

```ebnf
event-spec =
    event-name, { ".", modifier } ;
```

Example:

```text
click.stop.prevent
keydown.enter
mousedown.left
```

The event command value is a command identifier/string consumed by the component handler.

---

# Part XVII — Directive Interaction

## 65. Directive categories

For implementation purposes, directives fall into categories.

### Content

```text
x-text
x-html
x-children
x-pre
```

### Data binding

```text
x-attr
x-attr:*
x-prop:*
x-class:*
x-model
```

### Events

```text
x-on:*
```

### Structural

```text
x-if
x-elseif
x-else
x-for
x-recursive
x-once
```

### Structural auxiliaries

```text
x-key
x-recursive-wrapper
```

### Visibility

```text
x-show
```

A new implementation SHOULD model these categories explicitly.

---

## 66. Multiple structural directives

The reference compiler processes directives in raw attribute iteration order and some structural directives rewrite the same generated-code boundaries.

This makes combinations of multiple primary structural directives on one element potentially order-dependent.

Therefore a conforming template SHOULD NOT combine primary structural directives such as:

```text
x-if + x-for
x-if + x-recursive
x-for + x-recursive
x-once + x-for
```

on the same element unless a future specification explicitly defines the combination.

Prefer nesting:

```html
<template-like-wrapper x-if="state.visible">
    <li x-for="item in state.items">...</li>
</template-like-wrapper>
```

where an actual valid HTML/XTL element is used as the wrapper.

---

# Part XVIII — Error Model

## 67. Compile-time errors

A compiler SHOULD reject or diagnose:

- malformed interpolation;
- invalid expression tokens or syntax;
- unsupported JavaScript constructs such as calls, assignments, statements, and literals outside the XTemplate grammar;
- unknown identifiers when the expression context is statically known;
- malformed HTML that makes directive structure impossible to determine;
- unknown `x-*` directives;
- unknown `x:*` pseudo-elements;
- invalid `x-for` syntax;
- invalid `x-recursive` syntax;
- invalid `x-key` placement;
- invalid conditional-chain placement;
- non-assignable `x-model` targets;
- non-empty `x-text` / `x-html` elements;
- mutually incompatible structural directives;
- invalid dynamic-binding syntax.

Diagnostics SHOULD identify:

```text
template/file
element
directive
source position when available
reason
```

---

## 68. Runtime errors

Runtime errors can still occur from:

- unknown identifiers when the context cannot be validated statically;
- invalid operand kinds, division/modulo by zero, or non-finite numeric results;
- unsupported host values or member/index adapters;
- failed `x-model` writes through missing, null, or read-only containers;
- unsupported collection values;
- DOM property assignment errors;
- invalid event assumptions;
- user command-handler errors;
- unsafe or invalid raw HTML.

A conforming implementation MUST report these as XTemplate evaluation errors rather than leaking backend-specific JavaScript or .NET exceptions as
language semantics. Null member/index reads themselves are not errors; they evaluate to `null` as defined in section 7.6.

---

# Part XIX — Security

## 69. Restricted expressions and template trust

The restricted expression language prevents templates from invoking arbitrary JavaScript, accessing implicit host globals, or expressing assignments
and statements. It enables static validation, portable server-side evaluation, and code generation that does not depend on `eval` or `new Function`.

These restrictions reduce the authority of expression text but do not make an entire template inherently safe. Implementations MUST still validate
template structure, explicitly control the values and members exposed by the evaluation context, safely encode ordinary text and attributes, and
apply the security rules of the target renderer. Directives such as `x-html` retain independent injection risks.

---

## 70. Raw HTML

`x-html` is an explicit raw-HTML escape hatch.

Values inserted through `x-html` may create XSS vulnerabilities if sourced from untrusted content.

XTL itself does not sanitize HTML.

---

## 71. CSP and compilation

The historical browser compiler used:

```js
new Function(...)
```

to create the render function.

That historical mechanism requires CSP allowances equivalent to dynamic code evaluation and does not define the current expression language.

A server/build compiler can instead emit a normal JavaScript function, such as:

```js
templateHandler: (state, handler, invalidate, utils, i18n, renderCount) => {
    ...
}
```

The required expression pipeline is tokenize, parse, validate, build an AST, and then evaluate or emit code. A build compiler may emit a normal
JavaScript function, preserving XTL semantics while avoiding runtime dynamic-code evaluation and allowing a stricter Content Security Policy.

`eval` and `new Function` are neither expression-language features nor requirements of XTL.

---

# Part XX — Reference Compiler Function ABI

## 72. Render function

The current compilation target is conceptually:

```js
render(
    state,
    handler,
    invalidate,
    utils,
    i18n,
    renderCount
) => VNode[]
```

Where:

### `state`

Current component state.

### `handler(command, event)`

Dispatches an XTL event command.

### `invalidate()`

Requests another component render.

Primarily used by generated `x-model` update handlers.

### `utils`

XTL runtime utilities such as:

```text
createVDOM
toArray
toObject
toDynamicArgument
toDynamicProperty
getInputValue
```

### `i18n`

XShell internationalization object.

Its presence in the historical render-function ABI does not make it an expression global. A language profile may expose an adapted `i18n` value by
explicitly adding it to `ExpressionContext`; otherwise the identifier is unknown.

### `renderCount`

Number of prior renders.

Used by `x-once`.

---

# Part XXI — Compiler Implementation Blueprint

## 73. Minimal compatible compiler architecture

A compiler can be implemented as:

```text
XTemplateCompiler
├── HtmlFragmentParser
├── InterpolationParser
├── TemplateAstBuilder
├── TemplateValidator
├── DependencyCollector
├── ExpressionTokenizer
├── ExpressionParser
├── ExpressionValidator
└── RenderBackend
    ├── JavaScriptVDomBackend
    ├── DirectDomBackend
    └── ServerHtmlBackend
```

---

## 74. JavaScript VDOM compiler

A compiler targeting current XShell SHOULD generate code with semantics equivalent to:

```js
let _ifs = {};

return [
    // generated VNodes
];
```

However, it does not need to reproduce the exact source text or internal `_ifs` strategy.

Conformance is based on observable behavior, not byte-for-byte generated JavaScript.

The compiler MUST emit JavaScript from validated expression AST nodes. It MUST NOT assume source expression text is arbitrary valid JavaScript or
splice unparsed expression text into generated code. Backend helpers MAY be used to preserve XTemplate null propagation, access, equality, truthiness,
and numeric rules where JavaScript operators alone differ.

---

## 75. Direct interpreter

An interpreter may instead walk the AST at render time:

```text
renderNode(astNode, scope, parentContext)
```

It must still preserve:

- expression contexts and all expression-language semantics;
- branch selection;
- loop locals;
- recursive locals;
- keyed identity where DOM preservation matters;
- event command semantics;
- model write-back;
- `x-once`;
- `x-pre`;
- raw node/HTML behavior.

---

## 76. Server-side renderer

Every valid XTemplate expression in this language version MUST be evaluable by a C# or other server-side renderer without Node.js, a browser, `eval`,
`new Function`, a JavaScript interpreter, or arbitrary JavaScript execution. The renderer MUST evaluate the expression AST using the semantics in
section 7, rather than translating behavior to host-language shortcuts with different coercion or access rules.

A server-side HTML renderer can implement the subset of template and DOM semantics meaningful without a browser.

Potentially renderable:

```text
text/interpolation
x-text
x-if / x-elseif / x-else
x-for
x-recursive
x-class
x-show
attributes
```

Browser-only semantics require either metadata/hydration or omission:

```text
DOM properties
events
x-model
real DOM-node x-children
x-once reconciliation behavior
```

A server renderer must document how it handles browser-only features.

---

# Part XXII — Examples

## 77. Text and binding

```html
<article>
    <h1 x-text="state.title"></h1>
    <p>Hello {{ state.user.name }}</p>
    <a x-attr:href="state.url">Open</a>
</article>
```

---

## 78. Commands

```html
<button
    x-attr:disabled="state.saving"
    x-on:click="save">
    Save
</button>
```

The click dispatches:

```text
save
```

to the component command handler.

---

## 79. Conditional rendering

```html
<x-spinner x-if="state.loading"></x-spinner>
<x-error x-elseif="state.error"
         x-attr:message="state.error.message">
</x-error>
<div x-else>
    Ready
</div>
```

Exactly one branch is structurally rendered.

---

## 80. Conditional visibility

```html
<div x-show="state.expanded">
    Details
</div>
```

The node remains structurally present when hidden.

---

## 81. Positional loop

```html
<ul>
    <li x-for="item in state.items">
        {{ item.label }}
    </li>
</ul>
```

---

## 82. Keyed loop

```html
<ul>
    <li x-for="item in state.items"
        x-key="id">
        {{ item.label }}
    </li>
</ul>
```

The `id` property defines item identity across renders.

---

## 83. Indexed loop

```html
<ol>
    <li x-for="(item,index) in state.items">
        {{ index + 1 }}. {{ item.label }}
    </li>
</ol>
```

---

## 84. Recursive tree

```html
<ul>
    <li
        x-recursive="(item,index,indexAbsolute) in state.items"
        x-key="id"
        x-recursive-wrapper="ul">
        <span x-text="item.label"></span>
    </li>
</ul>
```

Each item's:

```text
item.children
```

is recursively rendered.

---

## 85. Class binding

```html
<a
    class="menuitem"
    x-class:selected="state.selectedId === item.id">
    {{ item.label }}
</a>
```

---

## 86. Property binding

```html
<x-datafield
    x-prop:domain="state.domain"
    x-model="state.value">
</x-datafield>
```

The domain remains an object/array property rather than being serialized as an HTML attribute.

---

## 87. Raw DOM node

```html
<span
    x-if="state.svg"
    x-children="state.svg">
</span>
```

---

## 88. Literal subtree

```html
<code x-pre>
    {{ state.thisWillNotExecute }}
</code>
```

---

# Part XXIII — Conformance Test Matrix

## 89. Parser tests

A conforming implementation should test:

```text
plain text
HTML comments
nested elements
custom elements
whitespace nodes
text interpolation
multiple interpolations
interpolation adjacent to text
literal braces under x-pre
```

### 89.1 Expression conformance tests

Expression suites MUST run the same cases against every parser/evaluator backend. At minimum, valid cases include:

| Expression | Context condition | Expected result or structure |
|---|---|---|
| `state.value` | `state.value = 2` | number `2` |
| `state.value + 1` | `state.value = 2` | number `3` |
| `state.value == 'a' || state.value == 'b'` | `state.value = 'b'` | boolean `true` |
| `state.array[state.index].var2 + 3 / 12` | index `0`; first item has `var2 = 4` | number `4.25` |
| `state.val1 ? '123' : '232'` | `state.val1 = false` | string `232` |
| `state.variable ?? 'default'` | member missing | string `default` |
| `state.enabled && !state.disabled` | enabled true, disabled false | boolean `true` |
| `(state.price * state.quantity) + state.tax` | price `2`, quantity `3`, tax `0.5` | number `6.5` |
| `item.name` | item name is `A` | string `A` |
| `state.items[index].name` | index `1`; second item name is `B` | string `B` |
| `state.user.name ?? 'Anonymous'` | `state.user = null` | string `Anonymous` |
| `1 == '1'` | none | boolean `false` |

Short-circuit tests MUST use a branch that would otherwise fail, proving that `false && (1 / 0)`, `true || (1 / 0)`, `1 ?? (1 / 0)`, and the
unselected branch of `true ? 1 : (1 / 0)` do not evaluate the division by zero.

At minimum, invalid cases include:

| Expression | Required rejection category |
|---|---|
| `state.getValue()` | method call |
| `Math.round(state.value)` | host global and call |
| `state.items.filter(x => x.enabled)` | method call and arrow function |
| `new Date()` | constructor |
| `window.location` | unknown identifier; no host-global fallback |
| `state.value = 10` | assignment |
| `state.value++` | update operator |
| `() => 1` | arrow function |
| `await state.value` | unsupported keyword/statement syntax |
| `import('module')` | dynamic import/call syntax |
| `[1, 2, 3]` | array literal |
| `{ value: 1 }` | object literal |

Assignable-expression tests MUST accept `state.name`, `state.user.name`, `state.items[index].value`, and `item.name`; they MUST reject arithmetic,
coalescing, and conditional expressions as `x-model` targets.

---

## 90. Binding tests

Test:

```text
static attributes
x-attr:name
boolean attributes
null attribute removal
x-attr object spread
dynamic attribute names
x-prop:name
kebab-to-camel property names
dynamic property names
static + dynamic classes
multiple x-class directives
```

Whole-object `x-prop` should remain marked unstable until standardized.

---

## 91. Conditional tests

Test:

```text
x-if true
x-if false
x-if/x-else
x-if/x-elseif/x-else
nested chains
multiple independent chains
stable logical positions when branch changes
```

---

## 92. Loop tests

Test:

```text
array
number
string
object keys
empty array
(item,index)
positional reconciliation
keyed reconciliation
insert keyed item
remove keyed item
reorder keyed item
duplicate keys
nested loops
```

---

## 93. Recursive tests

Test:

```text
single level
multiple levels
empty children
index
indexAbsolute
indent
x-key
x-recursive-wrapper
nested conditionals inside recursive item
```

---

## 94. Event tests

Test:

```text
click command
custom event
stop
prevent
enter
escape
arrow keys
modifier keys
mouse buttons
slotchange
```

---

## 95. Model tests

Test at least:

```text
custom component value
text input
number input
range
checkbox
radio
select
multiple select
nested assignable path
loop-local assignable path
change event invalidation
```

Radio and multiple-select expectations should be decided explicitly rather than copied from known questionable reference code.

---

## 96. Render-control tests

Test:

```text
x-once first render
x-once later renders
x-pre interpolation
x-pre nested directive-looking attributes
x-html
x-children node
x-children node array
x-children empty value
```

---

# Part XXIV — Known Reference-Implementation Quirks

## 97. Why this section exists

These behaviors are observable in the current source but should not silently become language design.

A compatibility implementation may need to know about them.

A clean new implementation should generally follow the normative semantics above and use tests to decide whether legacy compatibility is required.

---

## 98. Global textual interpolation preprocessing

The reference compiler performs a global string replacement of `{{` and `}}` before HTML parsing.

This is simple but not a robust interpolation tokenizer.

A new parser should recognize interpolation structurally while preserving equivalent normal-template behavior.

---

## 99. Conditional state indexed by nesting depth

The current compiler uses a condition register keyed by nesting depth.

This can make malformed non-contiguous `x-elseif` / `x-else` sequences behave as if associated with an earlier condition.

A validator should reject such templates.

---

## 100. Whole-object `x-prop`

As noted earlier, whole-object `x-prop` currently feeds the attribute map.

This appears inconsistent with the directive name and with `x-prop:name`.

Do not encode that behavior into a new language implementation unless strict legacy compatibility requires it.

---

## 101. Radio `x-model`

The render-side radio comparison hardcodes `state.value` in current generated code.

This fails to generalize to arbitrary model expressions.

Treat the intended behavior as comparison against the actual model target.

---

## 102. Multi-select `x-model`

Current compile-time code for the render-side multi-select property references `event`, which is not naturally defined in render scope.

The read/write representation is also inconsistent with ordinary arrays because `getInputValue` joins values with commas.

This area requires a deliberate future contract.

---

## 103. Mouse event modifier expressions

The current runtime contains conditions such as:

```js
!event.button == 0
```

whose JavaScript precedence does not cleanly express the intended condition.

The language semantics should be normal mouse-button filtering.

---

## 104. Alt-key typo

The current runtime checks a property resembling:

```text
altlKey
```

rather than the standard:

```text
altKey
```

This is a runtime defect.

---

## 105. `x-show` and style collisions

`x-show` contributes a `style` attribute through generated object merging.

Multiple generated `style` properties can produce source-order-sensitive behavior.

A future language version could define explicit style-object merging.

---

## 106. Structural directive combinations

Because the reference compiler rewrites generated prefix/suffix code while iterating attributes, multiple structural directives on one element can be source-order-sensitive.

A new compiler should normalize structural constructs in an AST and reject undefined combinations.

---

# Part XXV — Recommended Canonical XTL Profile

## 107. Canonical authoring forms

For new XShell code and generated templates, prefer:

```text
x-attr:name
x-prop:name
x-on:event
x-class:name
x-if / x-elseif / x-else
x-show
x-for
x-key
x-recursive
x-recursive-wrapper
x-model
x-once
x-pre
x-text
x-html
x-children
{{ expression }}
```

Avoid unstable/ambiguous forms such as whole-object `x-prop` until their semantics are formally resolved.

---

# Part XXVI — Implementation Checklist for an LLM

## 108. If asked to implement an XTL compiler

An implementation-generating LLM should perform these steps:

1. Parse XTL as an HTML fragment.
2. Tokenize and parse every expression-bearing construct into expression AST nodes using section 7.
3. Create a target-neutral AST.
4. Classify directives into content, bindings, events, structural directives, and auxiliaries.
5. Validate directive combinations.
6. Validate identifiers against the directive-specific expression context and reject unsupported syntax.
7. Introduce lexical scope for loop and recursive variables.
8. Normalize `x-for` sources according to XTL collection rules.
9. Implement contiguous conditional chains.
10. Implement positional and keyed list identity.
11. Implement recursive traversal through `item.children`.
12. Preserve static and conditional classes.
13. Distinguish DOM attributes from DOM properties.
14. Bind events to named commands.
15. Implement `x-model` as read + write-back + invalidation.
16. Implement raw HTML, raw node, once, and pre semantics.
17. Collect custom-element dependencies while respecting `x-lazy`.
18. Evaluate expression AST nodes or generate target code from them while preserving XTemplate semantics.
19. Add diagnostics rather than silently accepting malformed templates.
20. Run the conformance matrix in this document.

---

## 109. If targeting current XShell JavaScript VDOM

Additionally:

1. emit the render ABI:

   ```js
   (state, handler, invalidate, utils, i18n, renderCount) => VNode[]
   ```

2. use VNode shapes compatible with `utils.createVDOM`;
3. preserve logical `options.index`;
4. emit conditional placeholders;
5. emit `x-for-start` / `x-for-end` markers;
6. set `forType` to `position` or `key`;
7. set per-item `options.key` for keyed lists;
8. set `format: "html"` for `x-html`;
9. set `format: "node"` for `x-children`;
10. set `once: true` for post-first-render `x-once` placeholders;
11. produce normal event functions rather than dynamically compiling event strings;
12. emit JavaScript only from validated expression AST nodes and avoid `eval` / `new Function`.

---

# Part XXVII — Language Boundaries

## 110. XTL does not define

This specification intentionally does not define:

- component module structure;
- component contracts/manifests;
- state-engine internals;
- dependency injection;
- service worker behavior;
- module manifests;
- resource packaging;
- ZIP format;
- routing;
- page/layout/dialog configuration;
- CSS compilation;
- Markdown compilation.

Those systems may consume or produce XTL but are separate contracts.

---

# Part XXVIII — Versioning Guidance

## 111. Language version

The current repository does not expose a formal XTL language-version field.

Before introducing breaking changes, XShell should consider defining an explicit template-language version, for example:

```text
xtemplate: 1
```

or equivalent engine metadata.

This specification introduces the restricted expression language as a breaking change from the historical arbitrary-JavaScript implementation.
Implementations that support historical templates SHOULD identify that compatibility mode separately; they MUST NOT describe it as conforming to the
expression language in section 7.

Future changes that would benefit from versioning include:

- changing the restricted expression grammar or value semantics;
- redefining whole-object property spread;
- changing `x-model` representation;
- changing recursive-child semantics;
- adding or removing directives;
- changing whitespace/interpolation parsing.

---

# Part XXIX — Summary Contract

## 112. Minimal mental model

An X Template is:

> an HTML fragment whose elements may contain declarative XTL directives and restricted XTemplate expressions; expressions are parsed into a
> portable AST and evaluated only against an explicit context, while rendering produces DOM-equivalent content and preserves structural identity
> across conditional and repeated regions.

The most important distinctions are:

```text
{{ ... }} / x-text     → text
x-html                  → raw HTML
x-children              → real DOM nodes

x-attr:*                → DOM attributes
x-prop:*                → DOM properties
x-class:*               → conditional CSS classes

x-on:*                  → named commands

x-if/...                → structural condition
x-show                  → visibility only

x-for                   → repeated region
x-key                   → stable item identity
x-recursive             → recursive repeated region through .children

x-model                 → read/write binding

x-once                  → render once
x-pre                   → literal child subtree
```

A correct implementation should preserve these semantics even if it uses a completely different internal renderer from the current XShell VDOM implementation.

---

# Appendix A — Compact Directive Reference

| Construct | Purpose | Value |
|---|---|---|
| `{{ expr }}` | Text interpolation | restricted XTemplate expression |
| `x-text` | Text content | expression |
| `x-html` | Raw HTML content | expression |
| `x-children` | Real DOM node content | expression |
| `x-attr:name` | Dynamic attribute | expression |
| `x-attr` | Attribute spread | expression/object |
| `x-prop:name` | Dynamic property | expression |
| `x-on:event` | Event command | command name |
| `x-class:name` | Conditional class | expression |
| `x-if` | Conditional branch | expression |
| `x-elseif` | Conditional branch | expression |
| `x-else` | Fallback branch | none |
| `x-show` | CSS visibility | expression |
| `x-for` | Repetition | loop expression |
| `x-key` | Item identity | property name |
| `x-recursive` | Recursive repetition | recursive loop expression |
| `x-recursive-wrapper` | Child-level wrapper | tag name |
| `x-model` | Two-way model | assignable expression |
| `x-once` | Preserve after first render | none |
| `x-pre` | Literal child subtree | none |

---

# Appendix B — Stability Classification

## Stable/core

```text
interpolation
x-text
x-html
x-children
x-attr:name
x-prop:name
x-on:event
x-class:name
x-if
x-elseif
x-else
x-show
x-for
x-key
x-recursive
x-recursive-wrapper
x-model (basic value semantics)
x-once
x-pre
```

## Implemented but should receive dedicated tests

```text
dynamic x-attr:[...]
dynamic x-prop:[...]
attribute object expansion edge cases
event modifier combinations
```

## Ambiguous or defective in current reference code

```text
whole-object x-prop
radio x-model generalized target
multiple-select x-model
some mouse modifier checks
Alt modifier check
style collision with x-show
multiple structural directives on one element
```

---

# Appendix C — Reference Examples from the XShell Style

Typical XShell templates use patterns such as:

```html
<label x-if="state.label" x-text="state.label"></label>
```

```html
<x-icon
    x-if="state.icon"
    x-attr:icon="state.icon">
</x-icon>
```

```html
<x-button
    x-on:click="next"
    x-if="state.index < state.panels.length - 1">
</x-button>
```

```html
<li
    x-for="item in state.breadcrumb"
    x-class:empty="!item.label">
</li>
```

```html
<x-menuitem
    x-recursive="menuitem in state.menu"
    x-key="href"
    x-attr:label="menuitem.label">
</x-menuitem>
```

```html
<li
    x-recursive="menuitem in state.menu"
    x-key="href"
    x-recursive-wrapper="ul">
</li>
```

These examples illustrate the intended declarative style: template expressions compute values, while user interaction is routed through named commands.

---

**End of specification**
