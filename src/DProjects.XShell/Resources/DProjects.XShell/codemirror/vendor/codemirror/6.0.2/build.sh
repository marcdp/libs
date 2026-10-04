#!/usr/bin/env bash

set -e

# ------------------------------------------------------------
# 1. Update Ubuntu
# ------------------------------------------------------------

sudo apt update
sudo apt upgrade -y

sudo apt install -y \
    curl \
    ca-certificates \
    build-essential


# ------------------------------------------------------------
# 2. Install Node.js + npm
# ------------------------------------------------------------

curl -fsSL https://deb.nodesource.com/setup_24.x | sudo -E bash -

sudo apt install -y nodejs

echo
echo "Node.js:"
node --version

echo
echo "npm:"
npm --version


# ------------------------------------------------------------
# 3. Create build directory
# ------------------------------------------------------------

CURRENT_DIR=$(pwd)
BUILD_DIR="$HOME/codemirror-build"

mkdir -p "$BUILD_DIR"
cd "$BUILD_DIR"


# ------------------------------------------------------------
# 4. Initialize project
# ------------------------------------------------------------

if [ ! -f package.json ]; then
    npm init -y
fi


# ------------------------------------------------------------
# 5. Install esbuild
# ------------------------------------------------------------

npm install --save-dev esbuild


# ------------------------------------------------------------
# 6. Approve esbuild install script if required by npm
# ------------------------------------------------------------

if command -v npm >/dev/null 2>&1; then
    npm install-scripts approve esbuild 2>/dev/null || true
fi


# ------------------------------------------------------------
# 7. Install CodeMirror packages
# ------------------------------------------------------------

npm install \
    codemirror \
    @codemirror/state \
    @codemirror/view \
    @codemirror/commands \
    @codemirror/language \
    @codemirror/autocomplete \
    @codemirror/search \
    @codemirror/lint \
    @codemirror/lang-javascript \
    @codemirror/lang-json \
    @codemirror/lang-html \
    @codemirror/lang-css \
    @codemirror/lang-xml \
    @codemirror/lang-markdown


# ------------------------------------------------------------
# 8. Create browser ESM entry point
# ------------------------------------------------------------

cat > codemirror-entry.js <<'EOF'
//
// Main CodeMirror package
//

export {
    basicSetup,
    minimalSetup
} from "codemirror";


//
// State
//

export {
    EditorState,
    StateEffect,
    StateField,
    Compartment,
    Transaction,
    Prec
} from "@codemirror/state";


//
// View
//

export {
    EditorView,
    Decoration,
    ViewPlugin,
    ViewUpdate,
    WidgetType,
    keymap,
    lineNumbers,
    highlightActiveLine,
    highlightActiveLineGutter,
    drawSelection,
    dropCursor,
    rectangularSelection,
    crosshairCursor,
    highlightSpecialChars
} from "@codemirror/view";


//
// Commands
//

export {
    defaultKeymap,
    history,
    historyKeymap,
    indentWithTab
} from "@codemirror/commands";


//
// Language infrastructure
//

export {
    LanguageSupport,
    LRLanguage,
    syntaxHighlighting,
    defaultHighlightStyle,
    bracketMatching,
    indentOnInput,
    foldGutter,
    foldKeymap
} from "@codemirror/language";


//
// Autocomplete
//

export {
    autocompletion,
    completionKeymap
} from "@codemirror/autocomplete";


//
// Search
//

export {
    searchKeymap,
    highlightSelectionMatches
} from "@codemirror/search";


//
// Linting
//

export {
    linter,
    lintGutter,
    lintKeymap
} from "@codemirror/lint";


//
// JavaScript / TypeScript / JSX
//

export {
    javascript,
    javascriptLanguage,
    typescriptLanguage,
    jsxLanguage
} from "@codemirror/lang-javascript";


//
// JSON
//

export {
    json,
    jsonLanguage,
    jsonParseLinter
} from "@codemirror/lang-json";


//
// HTML
//

export {
    html,
    htmlLanguage
} from "@codemirror/lang-html";


//
// CSS
//

export {
    css,
    cssLanguage
} from "@codemirror/lang-css";


//
// XML
//

export {
    xml,
    xmlLanguage
} from "@codemirror/lang-xml";


//
// Markdown
//

export {
    markdown,
    markdownLanguage
} from "@codemirror/lang-markdown";
EOF


# ------------------------------------------------------------
# 9. Build single browser ESM bundle
# ------------------------------------------------------------

mkdir -p dist

./node_modules/.bin/esbuild codemirror-entry.js \
    --bundle \
    --format=esm \
    --platform=browser \
    --target=es2022 \
    --outfile=dist/codemirror.js


# ------------------------------------------------------------
# 10. Validate result
# ------------------------------------------------------------

echo
echo "Build completed:"
ls -lh dist/codemirror.js

echo
echo "Checking for remaining static imports..."

if grep -nE '^[[:space:]]*import[[:space:]]' dist/codemirror.js; then
    echo
    echo "WARNING: remaining import statements were found."
else
    echo "OK: no remaining static imports."
fi


echo
echo "Checking for esm.sh / Node.js shims..."

if grep -nE 'process\.mjs|/node/|esm\.sh' dist/codemirror.js; then
    echo
    echo "WARNING: unexpected external/shim references were found."
else
    echo "OK: no Node/esm.sh shim references found."
fi


echo
echo "Checking for eval/new Function..."

if grep -nE '\beval\s*\(|new[[:space:]]+Function\s*\(' dist/codemirror.js; then
    echo
    echo "WARNING: eval/new Function usage found."
else
    echo "OK: no eval/new Function usage found."
fi


# ------------------------------------------------------------
# 11. Copy final bundle to the original directory
# ------------------------------------------------------------

echo

cp "$BUILD_DIR/dist/codemirror.js" "$CURRENT_DIR/codemirror.js"

echo "Output:"
echo "  $CURRENT_DIR/codemirror.js"

pwd
``````bash
#!/usr/bin/env bash

set -e

# ------------------------------------------------------------
# 1. Update Ubuntu
# ------------------------------------------------------------

sudo apt update
sudo apt upgrade -y

sudo apt install -y \
    curl \
    ca-certificates \
    build-essential


# ------------------------------------------------------------
# 2. Install Node.js + npm
# ------------------------------------------------------------

curl -fsSL https://deb.nodesource.com/setup_24.x | sudo -E bash -

sudo apt install -y nodejs

echo
echo "Node.js:"
node --version

echo
echo "npm:"
npm --version


# ------------------------------------------------------------
# 3. Create build directory
# ------------------------------------------------------------

CURRENT_DIR=$(pwd)
BUILD_DIR="$HOME/codemirror-build"

mkdir -p "$BUILD_DIR"
cd "$BUILD_DIR"


# ------------------------------------------------------------
# 4. Initialize project
# ------------------------------------------------------------

if [ ! -f package.json ]; then
    npm init -y
fi


# ------------------------------------------------------------
# 5. Install esbuild
# ------------------------------------------------------------

npm install --save-dev esbuild


# ------------------------------------------------------------
# 6. Approve esbuild install script if required by npm
# ------------------------------------------------------------

if command -v npm >/dev/null 2>&1; then
    npm install-scripts approve esbuild 2>/dev/null || true
fi


# ------------------------------------------------------------
# 7. Install CodeMirror packages
# ------------------------------------------------------------

npm install \
    codemirror \
    @codemirror/state \
    @codemirror/view \
    @codemirror/commands \
    @codemirror/language \
    @codemirror/autocomplete \
    @codemirror/search \
    @codemirror/lint \
    @codemirror/lang-javascript \
    @codemirror/lang-json \
    @codemirror/lang-html \
    @codemirror/lang-css \
    @codemirror/lang-xml \
    @codemirror/lang-markdown


# ------------------------------------------------------------
# 8. Create browser ESM entry point
# ------------------------------------------------------------

cat > codemirror-entry.js <<'EOF'
//
// Main CodeMirror package
//

export {
    basicSetup,
    minimalSetup
} from "codemirror";


//
// State
//

export {
    EditorState,
    StateEffect,
    StateField,
    Compartment,
    Transaction,
    Prec
} from "@codemirror/state";


//
// View
//

export {
    EditorView,
    Decoration,
    ViewPlugin,
    ViewUpdate,
    WidgetType,
    keymap,
    lineNumbers,
    highlightActiveLine,
    highlightActiveLineGutter,
    drawSelection,
    dropCursor,
    rectangularSelection,
    crosshairCursor,
    highlightSpecialChars
} from "@codemirror/view";


//
// Commands
//

export {
    defaultKeymap,
    history,
    historyKeymap,
    indentWithTab
} from "@codemirror/commands";


//
// Language infrastructure
//

export {
    LanguageSupport,
    LRLanguage,
    syntaxHighlighting,
    defaultHighlightStyle,
    bracketMatching,
    indentOnInput,
    foldGutter,
    foldKeymap
} from "@codemirror/language";


//
// Autocomplete
//

export {
    autocompletion,
    completionKeymap
} from "@codemirror/autocomplete";


//
// Search
//

export {
    searchKeymap,
    highlightSelectionMatches
} from "@codemirror/search";


//
// Linting
//

export {
    linter,
    lintGutter,
    lintKeymap
} from "@codemirror/lint";


//
// JavaScript / TypeScript / JSX
//

export {
    javascript,
    javascriptLanguage,
    typescriptLanguage,
    jsxLanguage
} from "@codemirror/lang-javascript";


//
// JSON
//

export {
    json,
    jsonLanguage,
    jsonParseLinter
} from "@codemirror/lang-json";


//
// HTML
//

export {
    html,
    htmlLanguage
} from "@codemirror/lang-html";


//
// CSS
//

export {
    css,
    cssLanguage
} from "@codemirror/lang-css";


//
// XML
//

export {
    xml,
    xmlLanguage
} from "@codemirror/lang-xml";


//
// Markdown
//

export {
    markdown,
    markdownLanguage
} from "@codemirror/lang-markdown";
EOF


# ------------------------------------------------------------
# 9. Build single browser ESM bundle
# ------------------------------------------------------------

mkdir -p dist

./node_modules/.bin/esbuild codemirror-entry.js \
    --bundle \
    --format=esm \
    --platform=browser \
    --target=es2022 \
    --outfile=dist/codemirror.js


# ------------------------------------------------------------
# 10. Validate result
# ------------------------------------------------------------

echo
echo "Build completed:"
ls -lh dist/codemirror.js

echo
echo "Checking for remaining static imports..."

if grep -nE '^[[:space:]]*import[[:space:]]' dist/codemirror.js; then
    echo
    echo "WARNING: remaining import statements were found."
else
    echo "OK: no remaining static imports."
fi


echo
echo "Checking for esm.sh / Node.js shims..."

if grep -nE 'process\.mjs|/node/|esm\.sh' dist/codemirror.js; then
    echo
    echo "WARNING: unexpected external/shim references were found."
else
    echo "OK: no Node/esm.sh shim references found."
fi


echo
echo "Checking for eval/new Function..."

if grep -nE '\beval\s*\(|new[[:space:]]+Function\s*\(' dist/codemirror.js; then
    echo
    echo "WARNING: eval/new Function usage found."
else
    echo "OK: no eval/new Function usage found."
fi


# ------------------------------------------------------------
# 11. Copy final bundle to the original directory
# ------------------------------------------------------------

echo

cp "$BUILD_DIR/dist/codemirror.js" "$CURRENT_DIR/codemirror.js"

echo "Output:"
echo "  $CURRENT_DIR/codemirror.js"
