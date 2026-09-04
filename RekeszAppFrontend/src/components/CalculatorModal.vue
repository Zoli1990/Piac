<script setup>
import { computed, ref } from 'vue'

const emit = defineEmits(['close'])
const display = ref('0')
const previous = ref(null)
const operator = ref(null)
const waiting = ref(false)
const expression = ref('')

const buttons = [
  ['AC', 'clear'], ['+/-', 'sign'], ['%', 'percent'], ['÷', 'operator'],
  ['7', 'digit'], ['8', 'digit'], ['9', 'digit'], ['×', 'operator'],
  ['4', 'digit'], ['5', 'digit'], ['6', 'digit'], ['−', 'operator'],
  ['1', 'digit'], ['2', 'digit'], ['3', 'digit'], ['+', 'operator'],
  ['0', 'zero'], ['.', 'digit'], ['=', 'equals']
]

const number = computed(() => Number(display.value.replace(',', '.')))

function inputDigit(d) {
  if (waiting.value) { display.value = d; waiting.value = false; return }
  if (display.value === '0') display.value = d
  else if (display.value.length < 12) display.value += d
}
function inputDecimal() {
  if (waiting.value) { display.value = '0.'; waiting.value = false; return }
  if (!display.value.includes('.')) display.value += '.'
}
function clear() { display.value = '0'; previous.value = null; operator.value = null; waiting.value = false; expression.value = '' }
function sign() { if (display.value !== '0') display.value = display.value.startsWith('-') ? display.value.slice(1) : '-' + display.value }
function percent() { display.value = String(number.value / 100) }
function calculate(a, b, op) {
  if (op === '+') return a + b
  if (op === '−') return a - b
  if (op === '×') return a * b
  if (op === '÷') return b === 0 ? NaN : a / b
  return b
}
function chooseOperator(op) {
  const current = number.value
  if (operator.value && previous.value !== null && !waiting.value) {
    const result = calculate(previous.value, current, operator.value)
    display.value = Number.isFinite(result) ? String(Number(result.toFixed(10))) : 'Error'
    previous.value = Number(display.value)
  } else previous.value = current
  operator.value = op
  waiting.value = true
  expression.value = `${previous.value} ${op}`
}
function equals() {
  if (!operator.value || previous.value === null) return
  expression.value = `${previous.value} ${operator.value} ${number.value} =`
  const result = calculate(previous.value, number.value, operator.value)
  display.value = Number.isFinite(result) ? String(Number(result.toFixed(10))) : 'Error'
  previous.value = null; operator.value = null; waiting.value = true
}
function press(value, type) {
  if (display.value === 'Error' && type !== 'clear') clear()
  if (type === 'digit') value === '.' ? inputDecimal() : inputDigit(value)
  else if (type === 'zero') inputDigit('0')
  else if (type === 'clear') clear()
  else if (type === 'sign') sign()
  else if (type === 'percent') percent()
  else if (type === 'operator') chooseOperator(value)
  else if (type === 'equals') equals()
}
</script>

<template>
  <div class="calc-overlay" @click.self="emit('close')">
    <div class="calculator" role="dialog" aria-label="Számológép">
      <div class="calc-top"><button @click="emit('close')">Kész</button></div>
      <div class="expression">{{ expression }}&nbsp;</div>
      <div class="display">{{ display }}</div>
      <div class="keys">
        <button v-for="([value, type], i) in buttons" :key="i" :class="[type]" @click="press(value, type)">{{ value }}</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.calc-overlay { position: fixed; inset: 0; z-index: 100; background: rgba(0,0,0,.55); display:flex; align-items:center; justify-content:center; padding:20px; }
.calculator { width:min(330px,100%); background:#050505; border-radius:28px; padding:14px; box-shadow:0 18px 50px rgba(0,0,0,.4); }
.calc-top { height:28px; display:flex; justify-content:flex-end; }
.calc-top button { border:0; background:none; color:#ff9500; font-size:15px; cursor:pointer; }
.expression { color:rgba(255,255,255,.45); min-height:22px; text-align:right; padding:0 10px; font-size:16px; font-weight:400; overflow:hidden; white-space:nowrap; text-overflow:ellipsis; font-variant-numeric:tabular-nums; }
.display { color:white; height:70px; display:flex; align-items:flex-end; justify-content:flex-end; padding:0 8px 10px; font-size:48px; font-weight:300; overflow:hidden; font-variant-numeric:tabular-nums; }
.keys { display:grid; grid-template-columns:repeat(4,1fr); gap:9px; }
.keys button { height:62px; border:0; border-radius:50%; font-size:24px; cursor:pointer; background:#333; color:#fff; }
.keys button:active { filter:brightness(1.35); }
.keys .clear,.keys .sign,.keys .percent { background:#a5a5a5; color:#111; }
.keys .operator,.keys .equals { background:#ff9500; color:#fff; }
.keys .zero { grid-column:span 2; border-radius:31px; text-align:left; padding-left:24px; }
</style>
