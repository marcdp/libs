// contract
export const contract = {
    description: "Internationalization examples",
    events: {},
    properties: {},
    methods: {}
};


// page
export default {
    template: `

        <h2>Internationalization</h2>

        <p>
            XShell internationalization covers three different concerns:
            translating application UI strings, storing multilingual application values,
            and formatting dates and times using the active locale.
        </p>

        <pre x-pre><code>i18n.translate()
    → application UI strings

i18n.formatText()
    → multilingual application values

i18n.formatDateTime()
    → locale-aware date/time formatting</code></pre>


        <h3>Current configuration</h3>

        <table>
            <tbody>
                <tr>
                    <td><strong>Current language</strong></td>
                    <td>{{ state.lang }}</td>
                </tr>
                <tr>
                    <td><strong>Configured languages</strong></td>
                    <td>{{ state.languagesText }}</td>
                </tr>
            </tbody>
        </table>


        <x-divider></x-divider>


        <h3>UI translation</h3>

        <p>
            <code>i18n.translate()</code> translates application UI strings using the dictionary
            configured for the active language.
        </p>

        <p>
            If no translation exists, the original value is returned.
        </p>

        <pre x-pre><code>i18n.translate("{{ state.translationSource }}")</code></pre>

        <table>
            <tbody>
                <tr>
                    <td><strong>Source</strong></td>
                    <td>{{ state.translationSource }}</td>
                </tr>
                <tr>
                    <td><strong>Translated</strong></td>
                    <td>{{ state.translationResult }}</td>
                </tr>
            </tbody>
        </table>


        <h4>x-i18n</h4>

        <p>
            <code>x-i18n</code> is a declarative wrapper around <code>i18n.translate()</code>.
        </p>

        <pre x-pre><code>&lt;x-i18n text="{{ state.translationSource }}"&gt;&lt;/x-i18n&gt;</code></pre>

        <p>
            Result:
            <strong>
                <x-i18n x-attr:text="state.translationSource"></x-i18n>
            </strong>
        </p>


        <h4>Fallback</h4>

        <pre x-pre><code>i18n.translate("{{ state.fallbackSource }}")</code></pre>

        <p>
            Result: <strong>{{ state.fallbackResult }}</strong>
        </p>


        <x-divider></x-divider>


        <h3>Multilingual application values</h3>

        <p>
            Multilingual data is different from UI translation.
            XShell stores the localized variants inside one encoded value.
        </p>

        <pre x-pre><code>{{ state.multilingualValue }}</code></pre>

        <p>
            Calling <code>i18n.formatText(value)</code> selects the value corresponding
            to the active application language.
        </p>

        <table>
            <tbody>
                <tr>
                    <td><strong>Current language</strong></td>
                    <td>{{ state.lang }}</td>
                </tr>
                <tr>
                    <td><strong>Localized value</strong></td>
                    <td>{{ state.localizedValue }}</td>
                </tr>
            </tbody>
        </table>

        <h4>Explicit language</h4>

        <table>
            <thead>
                <tr>
                    <th>Language</th>
                    <th>Value</th>
                </tr>
            </thead>
            <tbody>
                <tr x-for="item in state.localizedValues">
                    <td>{{ item.label }} ({{ item.id }})</td>
                    <td>{{ item.value }}</td>
                </tr>
            </tbody>
        </table>

        <pre x-pre><code>i18n.formatText(value, "es")</code></pre>


        <x-divider></x-divider>


        <h3>Localized text field</h3>

        <p>
            <code>text_i18n</code> is used for short multilingual text values.
            The field displays one input for every configured language in
            <code>langs</code>.
        </p>

        <x-datafields>
            <x-datafield
                label="Name"
                type="text_i18n"
                x-model="state.textValue">
            </x-datafield>
        </x-datafields>

        <p>
            Stored value:
        </p>

        <pre x-pre><code>{{ state.textValue }}</code></pre>

         <x-json x-prop:value='state.textValue'></x-json>

        <h3>Localized textarea</h3>

        <p>
            <code>textarea_i18n</code> uses the same storage format but is intended
            for longer plain-text content.
        </p>

        <x-datafields>
            <x-datafield
                label="Description"
                type="textarea_i18n"
                x-model="state.textareaValue">
            </x-datafield>
        </x-datafields>

        <p>
            Stored value:
        </p>

        <pre x-pre><code>{{ state.textareaValue }}</code></pre>


        <h3>Localized rich text</h3>

        <p>
            <code>richtext_i18n</code> edits localized rich HTML content through
            <code>x-richtext</code>.
        </p>

        <x-datafields>
            <x-datafield
                label="Rich content"
                type="richtext_i18n"
                x-model="state.richtextValue">
            </x-datafield>
        </x-datafields>

        <p>
            Stored value:
        </p>

        <pre x-pre><code>{{ state.richtextValue }}</code></pre>


        <h3>Localized Markdown</h3>

        <p>
            <code>markdown_i18n</code> stores one Markdown source per language.
        </p>

        <x-datafields>
            <x-datafield
                label="Documentation"
                type="markdown_i18n"
                x-model="state.markdownValue">
            </x-datafield>
        </x-datafields>

        <p>
            Stored value:
        </p>

        <pre x-pre><code>{{ state.markdownValue }}</code></pre>


        <x-divider></x-divider>


        <h3>Date and time formatting</h3>

        <p>
            <code>i18n.formatDateTime()</code> uses the active language and the
            formats configured in <code>xshell.i18n.datetime</code>.
        </p>

        <p>
            Example value:
            <code>{{ state.datetime }}</code>
        </p>

        <table>
            <tbody>
                <tr>
                    <td><strong>Date</strong></td>
                    <td>{{ state.dateFormatted }}</td>
                </tr>
                <tr>
                    <td><strong>Time</strong></td>
                    <td>{{ state.timeFormatted }}</td>
                </tr>
                <tr>
                    <td><strong>Date and time</strong></td>
                    <td>{{ state.datetimeFormatted }}</td>
                </tr>
                <tr>
                    <td><strong>ISO</strong></td>
                    <td>{{ state.isoFormatted }}</td>
                </tr>
            </tbody>
        </table>

        <pre x-pre><code>i18n.formatDateTime(value, "date")
i18n.formatDateTime(value, "time")
i18n.formatDateTime(value, "datetime")
i18n.formatDateTime(value, "iso")</code></pre>


        <h4>x-datetime</h4>

        <table>
            <tbody>
                <tr>
                    <td><strong>Date</strong></td>
                    <td>
                        <x-datetime
                            x-attr:value="state.datetime"
                            format="date">
                        </x-datetime>
                    </td>
                </tr>

                <tr>
                    <td><strong>Time</strong></td>
                    <td>
                        <x-datetime
                            x-attr:value="state.datetime"
                            format="time">
                        </x-datetime>
                    </td>
                </tr>

                <tr>
                    <td><strong>Date and time</strong></td>
                    <td>
                        <x-datetime
                            x-attr:value="state.datetime"
                            format="datetime">
                        </x-datetime>
                    </td>
                </tr>
            </tbody>
        </table>


        <x-divider></x-divider>


        <h3>Summary</h3>

        <pre x-pre><code>i18n.translate()
    UI/application strings

x-i18n
    declarative UI translation

i18n.formatText()
    reads multilingual application values

text_i18n
    short localized plain text

textarea_i18n
    long localized plain text

richtext_i18n
    localized rich HTML content

markdown_i18n
    localized Markdown content

i18n.formatDateTime()
    locale-aware date/time formatting

x-datetime
    declarative date/time formatting</code></pre>
   
    `,


    state: {
        lang: "",
        languagesText: "",
        languageIds: [],

        translationSource: "Save",
        translationResult: "",

        fallbackSource: "This translation does not exist",
        fallbackResult: "",

        multilingualValue:
            "i18n:en=Hello|i18n:es=Hola|i18n:fr=Bonjour",

        localizedValue: "",
        localizedValues: [],

        textValue:
            "i18n:en=Product|i18n:es=Producto|i18n:fr=Produit",

        textareaValue:
            "i18n:en=This is a longer English description.|i18n:es=Esta es una descripción más larga.",

        richtextValue:
            "i18n:en=<p><strong>Hello</strong> from the rich text editor.</p>|i18n:es=<p><strong>Hola</strong> desde el editor de texto enriquecido.</p>",

        markdownValue:
            "i18n:en=# Hello\nThis is Markdown content.|i18n:es=# Hola\nEste es contenido Markdown.",

        datetime: "2026-09-30T18:30:00Z",

        dateFormatted: "",
        timeFormatted: "",
        datetimeFormatted: "",
        isoFormatted: ""
    },


    controller({ state, i18n }) {

        const refreshMultilingualValues = () => {
            state.localizedValue =
                i18n.formatText(state.multilingualValue);

            state.localizedValues =
                i18n.config.langs.map(lang => ({
                    id: lang.id,
                    label: lang.label,
                    value: i18n.formatText(
                        state.multilingualValue,
                        lang.id
                    )
                }));
        };


        return {
            load() {
                // configuration
                state.lang = i18n.config.lang;

                state.languageIds =
                    i18n.config.langs.map(lang => lang.id);

                state.languagesText =
                    i18n.config.langs
                        .map(lang =>
                            lang.label + " (" + lang.id + ")"
                        )
                        .join(", ");


                // UI translation
                state.translationResult =
                    i18n.translate(state.translationSource);

                state.fallbackResult =
                    i18n.translate(state.fallbackSource);


                // multilingual values
                refreshMultilingualValues();


                // date/time
                state.dateFormatted =
                    i18n.formatDateTime(
                        state.datetime,
                        "date"
                    );

                state.timeFormatted =
                    i18n.formatDateTime(
                        state.datetime,
                        "time"
                    );

                state.datetimeFormatted =
                    i18n.formatDateTime(
                        state.datetime,
                        "datetime"
                    );

                state.isoFormatted =
                    i18n.formatDateTime(
                        state.datetime,
                        "iso"
                    );
            }
        };
    }
};