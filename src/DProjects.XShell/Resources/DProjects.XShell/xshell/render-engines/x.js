import xshell from '../xshell.js';
import {rewriteTemplateAttribute} from "../utils/rewriteDocumentUrls.js";

class XTemplate {

	//fields
	_styleSheets = null;
	_render = null;
	_utils = null;

	//ctor
	constructor({ styleSheets = [], render, context = null }) {
		this._compile({ styleSheets, render, context });
	}

	//props
	get styleSheets() {return this._styleSheets;}
	get render() {return this._render;}
	get utils() {return this._utils;}
	
	//methods
	_compile({ styleSheets = [], render, context = null }) {
		//create array of styleSheets if required
		if (styleSheets != null && !Array.isArray(styleSheets)) {
			styleSheets = [styleSheets];
		}
		for (let i=0; i < styleSheets.length; i++) {
			let styleSheet = styleSheets[i];
			if (styleSheet.sheet) {
				styleSheets[i] = styleSheet.sheet;
			} else if (typeof(styleSheet)=="string") {
				const newStyleSheet = new CSSStyleSheet();
				newStyleSheet.replaceSync(styleSheet);
				styleSheets[i] = newStyleSheet;
			}
		}
		this._styleSheets = styleSheets;
		// bind source URL rewriting to this template's resource context
		this._utils = Object.create(utils);
		this._utils.rewriteAttribute = (tag, attrs, attr, value) => rewriteTemplateAttribute(tag, attrs, attr, value, context);
		// use only the server-compiled render code, avoiding runtime source parsing and code generation under strict CSP
		this._render = render;
	}
	//public methods
	createInstance(handler, invalidate, element) {
		return new XTemplateInstance(this, handler, invalidate, element);
	}

}
 

// utils
const emptyObject = {};
const emptyArray = [];
let freeId = 1;
const isStyleAttribute = (name) => typeof name === "string" && name.toLowerCase() === "style";
const utils = new class {
	createVDOM = (tag, attrs, props, styles, events, options, children, moreChildren) => {
		if (children && children.length && moreChildren) {
			let lastIndex = children[children.length - 1].options.index;
			for(let child of moreChildren) {
				child.options.index += lastIndex;
				children.push(child);
			}
		};
		return {
			tag: tag,
			attrs: attrs ?? emptyObject,
			props: props ?? emptyObject,
			styles: styles ?? emptyObject,
			events: events ?? emptyObject,
			options: options ?? { index: 0 },
			children: children ?? emptyArray
		};
	};
	expr = (() => {
		const currencies = new Map([["EUR", {digits:2, symbol:"€"}], ["USD", {digits:2, symbol:"$"}], ["JPY", {digits:0, symbol:"¥"}], ["GBP", {digits:2, symbol:"£"}], ["CAD", {digits:2, symbol:"CA$"}], ["AUD", {digits:2, symbol:"A$"}], ["CHF", {digits:2, symbol:"CHF"}], ["CNY", {digits:2, symbol:"CN¥"}], ["KRW", {digits:0, symbol:"₩"}]]);
		const patternTokens = ["yyyy", "MMMM", "MMM", "MM", "dd", "HH", "mm", "ss", "M", "d", "H"];
		const locale = (i18n) => {
			const value = i18n?.config?.lang;
			if (typeof value !== "string" || !value.trim()) return {kind:"invariant"};
			const name = value === "en" ? "en-US" : value === "es" ? "es-ES" : value === "tr" ? "tr-TR" : value;
			return {kind:"locale", name};
		};
		const fail = (message) => { throw new Error(`XTemplate runtime error: ${message}`); };
		const number = (value, message = "Numeric operand must be a finite number") => typeof value === "number" && Number.isFinite(value) ? value : fail(message);
		const normalize = (value) => value === undefined ? null : typeof value === "number" ? number(value, "XTemplate numbers must be finite") : value;
		const checked = (value) => Number.isFinite(value) ? value : fail("Arithmetic result must be a finite number");
		const digits = (value) => Number.isInteger(value) && value >= 0 && value <= 15 ? value : fail("Transformer digits must be a supported non-negative integer");
		const round = (value, precision) => {
			const factor = 10 ** precision;
			const scaled = Math.abs(value) * factor;
			// preserve large binary64 integer values whose decimal precision cannot change
			if (!Number.isFinite(scaled) || Math.abs(value) >= 1e21) return value;
			return Math.sign(value) * Math.floor(scaled + 0.5) / factor;
		};
		const invariantMonths = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
		const invariantShortMonths = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
		const expandExponent = (value) => {
			const [coefficient, exponent] = String(value).toLowerCase().split("e");
			if (exponent === undefined) return coefficient;
			const [whole, fraction = ""] = coefficient.split(".");
			const digits = whole + fraction;
			const decimal = whole.length + Number(exponent);
			if (decimal <= 0) return "0." + "0".repeat(-decimal) + digits;
			if (decimal >= digits.length) return digits + "0".repeat(decimal - digits.length);
			return digits.slice(0, decimal) + "." + digits.slice(decimal);
		};
		const invariantNumber = (value, precision, exact) => {
			const rounded = round(value, precision);
			const sign = rounded < 0 ? "-" : "";
			const absolute = Math.abs(rounded);
			let [integer, fraction = ""] = (absolute >= 1e21 ? expandExponent(absolute) : absolute.toFixed(precision)).split(".");
			if (absolute >= 1e21) fraction = "0".repeat(precision);
			integer = integer.replace(/\B(?=(\d{3})+(?!\d))/g, ",");
			if (!exact) fraction = fraction.replace(/0+$/, "");
			return sign + integer + (fraction ? "." + fraction : "");
		};
		const formattedNumber = (value, precision, exact, culture) => culture.kind === "invariant"
			? invariantNumber(value, precision, exact)
			: new Intl.NumberFormat(culture.name, {useGrouping:true, minimumFractionDigits:exact ? precision : 0, maximumFractionDigits:precision}).format(round(value, precision));
		const scalar = (value) => {
			if (value === null) return "";
			if (typeof value === "string") return value;
			if (typeof value === "boolean") return value ? "true" : "false";
			if (typeof value === "number" && Number.isFinite(value)) return String(value);
			return fail("Value cannot be converted to an XTemplate scalar");
		};
		const style = (name, value) => value === null ? {} : {[name]: {value: scalar(value), priority: ""}};
		const truthy = (value) => !(value === null || value === false || value === "" || (typeof value === "number" && value === 0));
		const own = (target, name) => Object.prototype.hasOwnProperty.call(target, name);
		const assignmentError = (message) => fail(`Model assignment error: ${message}`);
		const protectedMember = (name) => name === "__proto__" || name === "constructor" || name === "prototype";
		const strictMember = (target, name) => {
			if (target === null || typeof target !== "object" || Array.isArray(target)) return assignmentError("member target must be an object");
			if (typeof name !== "string" || protectedMember(name)) return assignmentError("member name is not writable");
			if (!own(target, name)) return assignmentError("member does not exist and member creation is unsupported");
			return target;
		};
		const writableMember = (target, name) => {
			strictMember(target, name);
			const descriptor = Object.getOwnPropertyDescriptor(target, name);
			if (!descriptor || (!descriptor.writable && typeof descriptor.set !== "function")) return assignmentError("member is read-only");
			return target;
		};
		const strictIndex = (target, index) => {
			if (!Array.isArray(target)) return assignmentError("numeric index target must be a collection");
			if (typeof index !== "number" || !Number.isFinite(index) || !Number.isInteger(index)) return assignmentError("collection index must be a finite integer number");
			if (index < 0) return assignmentError("collection index must be non-negative");
			if (index >= target.length) return assignmentError("collection index is out of range");
			return target;
		};
		const writableIndex = (target, index) => {
			strictIndex(target, index);
			const descriptor = Object.getOwnPropertyDescriptor(target, String(index));
			if (!descriptor || (!descriptor.writable && typeof descriptor.set !== "function")) return assignmentError("collection index is read-only");
			return target;
		};
		const compare = (left, right) => {
			if (typeof left === "number" && typeof right === "number") return left < right ? -1 : left > right ? 1 : 0;
			if (typeof left !== "string" || typeof right !== "string") return fail("Comparison requires two numbers or two strings");
			const leftValues = [...left];
			const rightValues = [...right];
			for (let index = 0; index < Math.min(leftValues.length, rightValues.length); index++) {
				const difference = leftValues[index].codePointAt(0) - rightValues[index].codePointAt(0);
				if (difference) return difference < 0 ? -1 : 1;
			}
			return leftValues.length - rightValues.length;
		};
		const parseDate = (value, allowDate) => {
			if (typeof value !== "string") return fail("Date/time transformers require a string input");
			let match = allowDate && /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
			if (match) return dateParts(+match[1], +match[2], +match[3], 0, 0, 0, 0, 0);
			match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(Z|[+-]\d{2}:\d{2})$/.exec(value);
			if (!match) return fail("Transformer received an invalid ISO-8601 value");
			const offset = match[7] === "Z" ? [0, 0] : [+match[7].slice(1, 3), +match[7].slice(4, 6)];
			return dateParts(+match[1], +match[2], +match[3], +match[4], +match[5], +match[6], offset[0], offset[1]);
		};
		const dateParts = (year, month, day, hour, minute, second, offsetHour, offsetMinute) => {
			const leap = year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0);
			const days = [31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
			if (year < 1 || month < 1 || month > 12 || day < 1 || day > days[month - 1] || hour > 23 || minute > 59 || second > 59 || offsetHour > 14 || offsetMinute > 59 || offsetHour === 14 && offsetMinute !== 0) return fail("Transformer received an invalid ISO-8601 value");
			return {year, month, day, hour, minute, second};
		};
		const pattern = (value, format, culture, dateAllowed, timeAllowed) => {
			if (typeof format !== "string") return fail("Transformer pattern requires a string argument");
			let found = false;
			const values = {yyyy:String(value.year).padStart(4, "0"), MM:String(value.month).padStart(2, "0"), M:String(value.month), dd:String(value.day).padStart(2, "0"), d:String(value.day), HH:String(value.hour).padStart(2, "0"), H:String(value.hour), mm:String(value.minute).padStart(2, "0"), ss:String(value.second).padStart(2, "0")};
			let result = "";
			for (let position = 0; position < format.length;) {
				const token = patternTokens.find(candidate => format.startsWith(candidate, position));
				if (token) {
					found = true;
					if (!dateAllowed && /^(yyyy|MMMM|MMM|MM|M|dd|d)$/.test(token)) return fail("Transformer pattern token is not allowed");
					if (!timeAllowed && /^(HH|H|mm|ss)$/.test(token)) return fail("Transformer pattern token is not allowed");
					result += token === "MMMM" ? culture.kind === "invariant" ? invariantMonths[value.month - 1] : new Intl.DateTimeFormat(culture.name, {month:"long", timeZone:"UTC"}).format(new Date(Date.UTC(2000, value.month - 1, 1))) : token === "MMM" ? culture.kind === "invariant" ? invariantShortMonths[value.month - 1] : new Intl.DateTimeFormat(culture.name, {month:"short", timeZone:"UTC"}).format(new Date(Date.UTC(2000, value.month - 1, 1))) : values[token];
					position += token.length;
					continue;
				}
				const character = String.fromCodePoint(format.codePointAt(position));
				if (/\p{L}/u.test(character)) return fail("Transformer pattern contains an unsupported token");
				result += character;
				position += character.length;
			}
			return found ? result : fail("Transformer pattern must contain a token");
		};
		const transform = (value, name, getArguments, i18n) => {
			if (value === null) return null;
			const args = getArguments();
			if (name === "number") {
				if (args.length > 1) return fail("Transformer 'number' received an invalid argument count");
				const culture = locale(i18n);
				const precision = args.length ? digits(args[0]) : 3;
				return formattedNumber(number(value, "Transformer 'number' requires a numeric input"), precision, args.length !== 0, culture);
			}
			if (name === "percent") {
				if (args.length > 1) return fail("Transformer 'percent' received an invalid argument count");
				const culture = locale(i18n);
				const precision = args.length ? digits(args[0]) : 3;
				const percentage = checked(number(value, "Transformer 'percent' requires a numeric input") * 100);
				if (culture.kind === "invariant") return formattedNumber(percentage, precision, args.length !== 0, culture) + "\u00A0%";
				return new Intl.NumberFormat(culture.name, {style:"percent", useGrouping:true, minimumFractionDigits:args.length ? precision : 0, maximumFractionDigits:precision}).format(round(percentage, precision) / 100);
			}
			if (name === "currency") {
				if (args.length < 1 || args.length > 2 || typeof args[0] !== "string" || !currencies.has(args[0])) return fail("Transformer 'currency' requires a supported uppercase ISO 4217 currency code");
				const culture = locale(i18n);
				const currency = currencies.get(args[0]);
				const precision = args.length === 2 ? digits(args[1]) : currency.digits;
				const amount = number(value, "Transformer 'currency' requires a numeric input");
				if (culture.kind === "invariant") {
					const formatted = formattedNumber(Math.abs(amount), precision, true, culture);
					const result = currency.symbol + formatted;
					return round(amount, precision) < 0 ? "(" + result + ")" : result;
				}
				const currencyFormatter = new Intl.NumberFormat(culture.name, {style:"currency", currency:args[0], currencyDisplay:"symbol", useGrouping:true, minimumFractionDigits:precision, maximumFractionDigits:precision});
				const parts = currencyFormatter.formatToParts(round(amount, precision));
				if (culture.name === "tr-TR") return parts.filter(part => part.type !== "currency").map(part => part.value).join("").replaceAll(" ", "\u00A0") + "\u00A0" + currency.symbol;
				return parts.map(part => part.type === "currency" ? currency.symbol : part.value).join("").replaceAll(" ", "\u00A0");
			}
			if (name === "upper" || name === "lower") {
				if (args.length || typeof value !== "string") return fail(`Transformer '${name}' requires a string input and no arguments`);
				const culture = locale(i18n);
				return culture.kind === "invariant" ? name === "upper" ? value.toUpperCase() : value.toLowerCase() : name === "upper" ? value.toLocaleUpperCase(culture.name) : value.toLocaleLowerCase(culture.name);
			}
			if (name === "trim") {
				if (args.length || typeof value !== "string") return fail("Transformer 'trim' requires a string input and no arguments");
				return value.trim();
			}
			if (name === "startsWith" || name === "endsWith" || name === "contains") {
				if (args.length !== 1 || typeof value !== "string" || typeof args[0] !== "string") return fail(`Transformer '${name}' requires a string input and one string argument`);
				return name === "startsWith" ? value.startsWith(args[0]) : name === "endsWith" ? value.endsWith(args[0]) : value.includes(args[0]);
			}
			if (name === "date" || name === "datetime" || name === "time") {
				if (args.length !== 1) return fail(`Transformer '${name}' requires one pattern argument`);
				const culture = locale(i18n);
				return pattern(parseDate(value, name === "date"), args[0], culture, name !== "time", name !== "date");
			}
			return fail(`Unknown transformer '${name}'`);
		};
		return {
			truthy, scalar, style,
			member: (target, name) => target === null ? null : (typeof target === "string" || Array.isArray(target)) ? name === "length" ? target.length : null : (typeof target === "object" && own(target, name) ? normalize(target[name]) : null),
			index: (target, index) => target === null ? null : typeof index === "string" ? (typeof target === "string" || Array.isArray(target) ? (index === "length" ? target.length : null) : (typeof target === "object" && own(target, index) ? normalize(target[index]) : null)) : (Number.isInteger(index) && index >= 0 && Array.isArray(target) ? normalize(target[index]) : fail("A collection index must be a non-negative integer number")),
			assign: (root, path, value) => {
				if (root === null || typeof root !== "object" || !Array.isArray(path) || path.length === 0) return assignmentError("requires a writable target");
				let target = root;
				for (let position = 0; position < path.length; position++) {
					const segment = path[position];
					const isFinal = position === path.length - 1;
					if (segment.kind === "member") {
						(isFinal ? writableMember : strictMember)(target, segment.name);
						if (isFinal) { target[segment.name] = value; return value; }
						target = normalize(target[segment.name]);
					} else if (segment.kind === "index") {
						const index = segment.value();
						if (typeof index === "string") {
							(isFinal ? writableMember : strictMember)(target, index);
							if (isFinal) { target[index] = value; return value; }
							target = normalize(target[index]);
						} else {
							(isFinal ? writableIndex : strictIndex)(target, index);
							if (isFinal) { target[index] = value; return value; }
							target = normalize(target[index]);
						}
					} else return assignmentError("requires a valid path segment");
					if (!isFinal && target === null) return assignmentError("intermediate target is null or missing");
				}
				return assignmentError("requires a writable target");
			},
			not: (value) => !truthy(value), unaryPlus: (value) => number(value), unaryMinus: (value) => checked(-number(value)),
			add: (left, right) => typeof left === "number" && typeof right === "number" ? checked(left + right) : (typeof left === "string" || typeof right === "string" ? scalar(left) + scalar(right) : fail("Operator '+' requires two numbers or a string operand")),
			subtract: (left, right) => checked(number(left) - number(right)), multiply: (left, right) => checked(number(left) * number(right)), divide: (left, right) => { const divisor = number(right); return divisor === 0 ? fail("Division by zero") : checked(number(left) / divisor); }, modulo: (left, right) => { const divisor = number(right); return divisor === 0 ? fail("Modulo by zero") : checked(number(left) % divisor); },
			equal: (left, right) => left === right, notEqual: (left, right) => left !== right, less: (left, right) => compare(left, right) < 0, lessOrEqual: (left, right) => compare(left, right) <= 0, greater: (left, right) => compare(left, right) > 0, greaterOrEqual: (left, right) => compare(left, right) >= 0,
			and: (left, right) => { const value = left(); return truthy(value) ? right() : value; }, or: (left, right) => { const value = left(); return truthy(value) ? value : right(); }, coalesce: (left, right) => { const value = left(); return value === null ? right() : value; }, conditional: (condition, whenTrue, whenFalse) => truthy(condition()) ? whenTrue() : whenFalse(),
			transform, collection: (value) => { if (value === null) return []; if (Array.isArray(value)) return value; if (typeof value === "number" && Number.isInteger(value) && value >= 0) return Array.from({length:value}, (_, index) => index + 1); if (typeof value === "string") return [...value]; if (typeof value === "object") return Object.keys(value); return fail("XTemplate collection source must be null, a collection, string, object, or non-negative integer number"); },
			attributes: (value) => { if (value === null || typeof value !== "object" || Array.isArray(value)) return fail("x-attr requires an object"); const result = {}; for (const key of Object.keys(value)) { if (isStyleAttribute(key)) return fail("Generic attributes cannot target 'style'"); const item = value[key]; if (typeof item === "string" || typeof item === "number" || item === true) result[key] = item; } return result; }, properties: (value) => value !== null && typeof value === "object" && !Array.isArray(value) ? Object.fromEntries(Object.keys(value).map((key) => [key, value[key] === undefined ? null : value[key]])) : fail("x-prop requires an object"), dynamicArgument: (name, value) => typeof name !== "string" ? fail("Dynamic attribute name must be a string") : isStyleAttribute(name) ? fail("Generic attributes cannot target 'style'") : {[name]: value}, dynamicProperty: (name, value) => typeof name === "string" ? {[name]: value} : fail("Dynamic property name must be a string")
		};
	})();
	getFreeId() {
		return "id" + freeId++;
	};
	getInputValue(target) {
		let value = target.value;
		if (target.localName == "input") {
			if (target.type == "number") {
				value = target.valueAsNumber; 
			} else if (target.type == "range") {
				value = target.valueAsNumber; 
			} else if (target.type == "checkbox") {
				value = target.checked; 
			} else if (target.type == "radio") {
				value = target.value; 
			}
		} else if (target.localName == "select") {
			value = Array.from(target.selectedOptions).map(option => option.value).join(',')
		}
		return value;
	}
};


// class
class XTemplateInstance {

	//fields
	_xtemplate = null;
	_element = null;
	_handler = null;
	_invalidate = null;

	//work fields
	_renderCount = 0;
	_vdom = null;

	//ctor
	constructor(xtemplate, handler, invalidate, element) {
		this._xtemplate = xtemplate;
		this._element = element;
		if (xtemplate.styleSheets.length) this._element.adoptedStyleSheets = xtemplate.styleSheets;
		this._handler = handler;
		this._invalidate = invalidate;
	}

	// methods
	render(state) {
		//render vdom
		let vdom = this._xtemplate.render(state, this._handler, this._invalidate, this._xtemplate.utils, xshell.i18n, this._renderCount++);
		//render vdom to dom
		if (this._vdom == null) {
			let index = 0;
			let documentFragment = document.createDocumentFragment();
			for (let vNode of vdom) {
				while (index < vNode.options.index) {
					let comment = document.createComment("");
					documentFragment.appendChild(comment);
					index++;
				}
				let element = this._createDomElement(vNode);
				documentFragment.appendChild(element);
				index++;
			}

			this._element.appendChild(documentFragment);			
			this._vdom = vdom;
		} else {
			this._diffDom(this._vdom, vdom, this._element, 0);
			this._vdom = vdom;
		}
	}
	_createDomElement(vNode) {
		//create element from vdom node        
		if (vNode.tag == "#text") {
			return document.createTextNode(vNode.children);
		} else if (vNode.tag == "#comment") {
			return document.createComment(vNode.children);
		} else {
			let el = document.createElement(vNode.tag);
			for (let attr in vNode.attrs) {
				if (isStyleAttribute(attr)) throw new Error("XTemplate generic attributes cannot target 'style'.");
				let attrValue = vNode.attrs[attr];
				if (attrValue == null) {
				} else if (typeof (attrValue) == "boolean") {
					if (attrValue) {
						el.setAttribute(attr, "");
					}
				} else if (typeof (attrValue) == "object") {
					let aux = [];
					for(let key in attrValue) {
						if (attrValue[key]) aux.push(key);
					}
					el.setAttribute(attr, aux.join(" "));
				} else {
					el.setAttribute(attr, attrValue);
				}
			}
			// apply compiler-structured styles through CSSOM without creating a style attribute
			for (let name in vNode.styles) {
				let style = vNode.styles[name];
				el.style.setProperty(name, style.value, style.priority);
			}
			for (let prop in vNode.props) {
				let propValue = vNode.props[prop];
				if (typeof(propValue) == "function") {
					propValue = propValue.call(vNode);
				}
				//if (!el.hasOwnProperty(prop)) {
				if (el[prop] === undefined) {
					//the property does not exist in the element yet
					if (!el._propertiesToInitialize) el._propertiesToInitialize = [];
					el._propertiesToInitialize.push({name: prop, value: propValue});
				} else {
					el[prop] = propValue;
				}
			}
			for (let event in vNode.events) {
				let eventHandler = vNode.events[event];
				let name = event;
				let options = {};
				if (name.indexOf(".") != -1) {
					let modifiers = name.split(".").slice(1);
					for (let modifier of modifiers) { //https://v2.vuejs.org/v2/guide/events
						options[modifier] = true;
					}
					name = name.substring(0, name.indexOf("."));
				}
				if (typeof (eventHandler) == "string") throw new Error("XTemplate event handlers must be precompiled functions.");
				el.addEventListener(name, (event, ...args) => {
					//mouse button
					if (options.left && event.button !== 0) return false;
					if (options.middle && event.button !== 1) return false;
					if (options.right && event.button !== 2) return false;
					//keys
					if (options.alt && !event.altKey) return false;
					if (options.shift && !event.shiftKey) return false;
					if (options.ctrl && !event.ctrlKey) return false;
					if (name == "keydown" || name == "keypress" || name == "keyup") {
						if (options.escape && event.key != "Escape") return false;
						if (options.enter && event.key != "Enter") return false;
						if (options.tab && event.key != "Tab") return false;
						if (options.backspace && event.key != "Backspace") return false;
						if (options.delete && event.key != "Delete") return false;
						if (options.space && event.key != " ") return false;
						if (options.up && event.key != "ArrowUp") return false;
						if (options.down && event.key != "ArrowDown") return false;
						if (options.left && event.key != "ArrowLeft") return false;
						if (options.right && event.key != "ArrowRight") return false;
					}
					//invoke                    
					let result = eventHandler.call(this, event, ...args);
					//stop, prevent
					if (options.stop) event.stopPropagation();
					if (options.prevent) event.preventDefault();
					//return
					return result;
				}, options);
			}
			if (vNode.options.format == 'node') {
				if (Array.isArray(vNode.children)) {
					el.replaceChildren();
					for(let element of vNode.children) {
						el.appendChild(element);
					}
				} else if (vNode.children) {
					if (el.firstChild != vNode.children) {
						if (el.firstChild) el.replaceChildren();
						el.appendChild(vNode.children);
					}
				} else {
					el.replaceChildren();
				}
			} else if (Array.isArray(vNode.children)) {
				let index = 0;
				for (let child of vNode.children) {
					while (index < child.options.index) {
						let comment = document.createComment("");
						el.appendChild(comment);
						index++;
					}
					let childElement = this._createDomElement(child);
					el.appendChild(childElement);
					index++;
				}
			} else if (typeof (vNode.children) == "string") {
				if (vNode.options.format == 'html') {
					el.innerHTML = vNode.children;
				} else {
					el.textContent = vNode.children;
				}
			}
			if (true) el.setAttribute("x-index", vNode.options.index);
			return el;
		}
	}
	_diffDom(vNodesOld, vNodesNew, parent, level) {
		//apply differences between two arrays of vdom elements
		let iold = 0;
		let inew = 0;
		for (let i = 0; ; i++) {
			let vNodeOld = vNodesOld[i + iold] ?? null;
			let vNodeNew = vNodesNew[i + inew] ?? null;
			if (vNodeOld == null && vNodeNew == null) {
				//nothing to do
				break;
			} else if (vNodeOld == null) {
				//append
				let element = this._createDomElement(vNodeNew);
				parent.appendChild(element);
			} else if (vNodeNew == null) {
				//remove
				parent.removeChild(parent.lastChild);
			} else if (vNodeOld.options.index < vNodeNew.options.index) {
				//remove old node
				let comment = document.createComment("");
				parent.replaceChild(comment, parent.childNodes[vNodeOld.options.index + inew]);
				inew--;
			} else if (vNodeOld.options.index > vNodeNew.options.index) {
				//replace node
				let element = this._createDomElement(vNodeNew);
				parent.replaceChild(element, parent.childNodes[vNodeNew.options.index + inew]);
				iold--;
			} else if (vNodeOld.tag == "#comment" && vNodeOld.options.forType == "key" && vNodeNew.tag == "#comment" && vNodeNew.options.forType == "key") {
				//for loop by key
				let oldStartIndex = i + iold;
				let oldEndIndex = oldStartIndex;
				while (vNodesOld[oldEndIndex].children != 'x-for-end') oldEndIndex++;
				let newStartIndex = i + inew;
				let newEndIndex = newStartIndex;
				while (vNodesNew[newEndIndex].children != 'x-for-end') newEndIndex++;
				this._diffDomListByKey(vNodesOld, oldStartIndex, oldEndIndex, vNodesNew, newStartIndex, newEndIndex, parent, level + 1);
				iold += oldEndIndex - oldStartIndex;
				inew += newEndIndex - newStartIndex;
			} else if (vNodeOld.tag == "#comment" && vNodeOld.options.forType == "position" && vNodeNew.tag == "#comment" && vNodeNew.options.forType == "position") {
				//for loop by position
				let oldStartIndex = i + iold;
				let oldEndIndex = oldStartIndex;
				while (vNodesOld[oldEndIndex].children != 'x-for-end') oldEndIndex++;
				let newStartIndex = i + inew;
				let newEndIndex = newStartIndex;
				while (vNodesNew[newEndIndex].children != 'x-for-end') newEndIndex++;
				this._diffDomListByPosition(vNodesOld, oldStartIndex, oldEndIndex, vNodesNew, newStartIndex, newEndIndex, parent, -999, level + 1);
				iold += oldEndIndex - oldStartIndex;
				inew += newEndIndex - newStartIndex;
			} else if (vNodeOld.tag != vNodeNew.tag) {
				//replace node
				let element = this._createDomElement(vNodeNew);
				parent.replaceChild(element, parent.childNodes[vNodeNew.options.index + inew]);
			} else if (vNodeOld.tag == "slot" && vNodeNew.tag == "slot") {
				//slot
			} else {
				//diff node
				let child = parent.childNodes[vNodeNew.options.index + inew];
				this._diffDomElement(vNodeOld, vNodeNew, child, level + 1);
			}
		}
	}
	_diffDomElement(vNodeOld, vNodeNew, element, level) {
		if (vNodeNew.options.once) {
			return;
		}
		//attrs
		let validAttrs = [];
		for (let attr in vNodeNew.attrs) {
			if (isStyleAttribute(attr)) throw new Error("XTemplate generic attributes cannot target 'style'.");
			let attrValue = vNodeNew.attrs[attr];
			if (attrValue != vNodeOld.attrs[attr]) {
				if (typeof (attrValue) == "boolean") {
					if (attrValue) {
						element.setAttribute(attr, "");
					} else {
						element.removeAttribute(attr);
					}
				} else if (typeof (attrValue) == "object") {
					let aux = [];
					for(let key in attrValue) {
						if (attrValue[key]) aux.push(key);
					}
					element.setAttribute(attr, aux.join(" "));
				} else if (attrValue == null) {
					element.removeAttribute(attr);
				} else {
					element.setAttribute(attr, attrValue);
				}
			}
			validAttrs.push(attr);
		}
		for (let attr in vNodeOld.attrs) {
			if (validAttrs.indexOf(attr) == -1) {
				element.removeAttribute(attr);
			}
		}
		// reconcile only style properties owned by the old and new XTemplate VNodes
		for (let name in vNodeNew.styles) {
			let oldStyle = vNodeOld.styles[name];
			let newStyle = vNodeNew.styles[name];
			if (!oldStyle || oldStyle.value !== newStyle.value || oldStyle.priority !== newStyle.priority) element.style.setProperty(name, newStyle.value, newStyle.priority);
		}
		for (let name in vNodeOld.styles) {
			if (!Object.prototype.hasOwnProperty.call(vNodeNew.styles, name)) element.style.removeProperty(name);
		}
		//props  
		let validProps = [];
		for (let prop in vNodeNew.props) {
			let propValue = vNodeNew.props[prop];
			if (typeof(propValue)=="function") {
				propValue = propValue.call(vNodeNew);
			}
			if (propValue != vNodeOld.props[prop]) {
				element[prop] = propValue;
			}
			validProps.push(prop);
		}
		//children
		if (vNodeNew.options.format == 'node') {
			if (Array.isArray(vNodeNew.children)) {
				element.replaceChildren();
				for(let childElement of vNodeNew.children) {
					element.appendChild(childElement);
				}
			} else if (vNodeNew.children) {
				if (element.firstChild != vNodeNew.children) {
					if (element.firstChild) element.replaceChildren();
					element.appendChild(vNodeNew.children);
				}
			} else {
				element.replaceChildren();
			}
		} else if (Array.isArray(vNodeNew.children)) {
			this._diffDom(vNodeOld.children, vNodeNew.children, element, level + 1);
		} else if (typeof (vNodeNew.children) == "string") {
			if (vNodeOld.children != vNodeNew.children) {
				if (vNodeNew.options.format == 'html') {
					element.innerHTML = vNodeNew.children;
				} else if (vNodeNew.options.format == 'json') {
					element.textContent = JSON.stringify(vNodeNew.children);
				} else {
					element.textContent = vNodeNew.children;
				}
			}
		}
	}
	_diffDomListByPosition(vNodesOld, oldStartIndex, oldEndIndex, vNodesNew, newStartIndex, newEndIndex, parent, parentBaseIndex, level) {
		//diff by position
		let oldLength = oldEndIndex - 1 - oldStartIndex;
		let newLength = newEndIndex - 1 - newStartIndex;
		let parentChildrenDesp = 0;
		for (let i = 0; i < newLength; i++) {
			let vNodeOld = vNodesOld[oldStartIndex + 1 + i];
			if (oldStartIndex + 1 + i >= oldEndIndex) vNodeOld = null;
			let vNodeNew = vNodesNew[newStartIndex + 1 + i];
			if (vNodeOld == null) {
				let element = this._createDomElement(vNodeNew);
				parent.insertBefore(element, parent.childNodes[newStartIndex + 1 + i + parentChildrenDesp]);
			} else {
				let element = parent.childNodes[newStartIndex + 1 + i + parentChildrenDesp];
				this._diffDomElement(vNodeOld, vNodeNew, element, level + 1);
			}        
		}
		while (newLength < oldLength) {
			let element = parent.childNodes[newStartIndex + 1 + newLength + parentChildrenDesp];
			parent.removeChild(element);
			oldLength--;
		}        
	}
	_diffDomListByKey(vNodesOld, oldStartIndex, oldEndIndex, vNodesNew, newStartIndex, newEndIndex, parent, level) {
		//get oldKeys and newKeys
		let oldKeys = [];
		for (let i = oldStartIndex + 1; i < oldEndIndex; i++) {
			oldKeys.push(vNodesOld[i].options.key);
		}
		let newKeys = [];
		for (let i = newStartIndex + 1; i < newEndIndex; i++) {
			newKeys.push(vNodesNew[i].options.key);
		}       
		//check for duplicates
		const duplicates = newKeys.filter((item, index) => newKeys.indexOf(item) !== index);
		if (duplicates.length) {
			console.warn(`Duplicated keys detected in x-for loop: ${duplicates}`);
		}
		//remove old keys, and old DOM elements
		let removeKeys = [];
		for (let i = oldKeys.length - 1; i >= 0; i--) {
			let key = oldKeys[i];
			if (newKeys.indexOf(key) == -1) {
				parent.removeChild(parent.childNodes[newStartIndex + i + 1]);
				removeKeys.push(key);
				oldKeys.splice(i, 1);
			}
		}
		//add new keys, and new DOM elements
		let addedKeys = [];
		for (let i = 0; i < newKeys.length; i++) {
			let key = newKeys[i];
			if (oldKeys.indexOf(key) == -1) {
				let element = this._createDomElement(vNodesNew[newStartIndex + i + 1]);
				parent.insertBefore(element, parent.childNodes[newStartIndex + i + 1]);
				oldKeys.splice(i, 0, key);
				addedKeys.push(key);
			}
		}
		//reorder keys and DOM elements
		for (let i = 0; i < newKeys.length; i++) {
			let key = newKeys[i];
			let newIndex = i;
			let oldIndex = oldKeys.indexOf(key);
			if (newIndex != oldIndex) {
				parent.insertBefore(parent.childNodes[oldStartIndex + oldIndex + 1], parent.childNodes[oldStartIndex + newIndex + 1]);
				oldKeys.splice(oldIndex, 1);
				oldKeys.splice(newIndex, 0, key);
				let old = vNodesOld.splice(oldStartIndex + 1 + oldIndex, 1);
				vNodesOld.splice(oldStartIndex + 1 + newIndex, 0, old[0]);
			}
		}
		//for each element in list, apply the differences
		for (let i = 0; i < newKeys.length; i++) {
			let key = newKeys[i];
			if (addedKeys.indexOf(key) == -1) {
				let vNodeOld = vNodesOld[oldStartIndex + 1 + i];
				let vNodeNew = vNodesNew[newStartIndex + 1 + i];
				let element = parent.childNodes[newStartIndex + 1 + i];
				this._diffDomElement(vNodeOld, vNodeNew, element, level);
			}
		}
	}

}



// export
export { utils as XTemplateRuntimeUtils };
export class RenderEngineX {
	
	// vars+
	_host = null;
	_state = null;
	_xtemplateInstance = null;

	// ctor
	constructor({ host, xtemplate, state, handler, invalidate }){
		this._host = host;
		this._xtemplateInstance = xtemplate.createInstance(
			(command, event) => {
				//handler
				handler(command, {event});
			}, 
			() => {
				//invalidate
				invalidate();
			}, 
			this._host
		);
		this._state = state;
	}

	// methods
	mount() {
		this._host.replaceChildren();
	}
    render() {
		this._xtemplateInstance.render(this._state);
		this._renderCount++;
    }
	unmount() {
		this._host.replaceChildren();
	}
}
export default function createRenderEngineFactoryX(template, context, templateRenderer) {
	// retain template only as the first parameter of the common render-engine interface
	void template;
	if (!templateRenderer || typeof(templateRenderer) !== "object" || typeof(templateRenderer.render) !== "function" ||
		!Array.isArray(templateRenderer.dependencies) || !Array.isArray(templateRenderer.slots)) {
		throw new Error("XTemplate requires a server-precompiled templateRenderer artifact with render, dependencies, and slots.");
	}
	// normalize compiler metadata without inspecting raw XTemplate source
	const componentLazy = typeof(context.componentLazy) === "string" ? context.componentLazy.toLowerCase() : null;
	const dependencies = new Set();
	for (const dependency of templateRenderer.dependencies) {
		if (!dependency || typeof(dependency.resource) !== "string" || !Array.isArray(dependency.ancestorPaths)) {
			throw new Error("XTemplate templateRenderer contains invalid dependency metadata.");
		}
		if (!componentLazy || dependency.ancestorPaths.some(path => Array.isArray(path) && !path.includes(componentLazy))) dependencies.add(dependency.resource);
	}
	if (!templateRenderer.slots.every(slot => typeof(slot) === "string")) throw new Error("XTemplate templateRenderer contains invalid slot metadata.");
	const slots = [...new Set(templateRenderer.slots)];
	let xtemplate = null;
	// return
	return {
		dependencies: Object.freeze([...dependencies]),
		slots: Object.freeze(slots),
		init: () => {
			// xtemplate
			xtemplate = new XTemplate({
				styleSheets: [],
				render: templateRenderer.render,
				context: context
			})
		},
		create: ({host, state, handler, invalidate}) => {
			return new RenderEngineX({ host, xtemplate, state, handler, invalidate });
		}
	};
}
