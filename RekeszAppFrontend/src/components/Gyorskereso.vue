<script setup>
import { computed, ref, watch } from 'vue'
import client from '../api/client'

const props = defineProps({
  modelValue: { type: [Number, String], default: '' },
  items: { type: Array, required: true },
  placeholder: { type: String, default: 'Kezdj el gépelni…' },
  postPath: { type: String, required: true },
  mentesLabel: { type: String, default: 'Mentés' },
  extra: { type: Object, default: () => ({}) }
})
const emit = defineEmits(['update:modelValue', 'created'])

const keres = ref('')
const nyitva = ref(false)
const hiba = ref('')

watch(() => props.modelValue, id => {
  const found = props.items.find(x => x.id === Number(id))
  keres.value = found ? found.nev : ''
}, { immediate: true })

const szurtLista = computed(() => {
  const q = keres.value.trim().toLowerCase()
  if (!q) return []
  return props.items.filter(x => x.nev.toLowerCase().startsWith(q)).slice(0, 8)
})

function onInput() {
  hiba.value = ''
  nyitva.value = true
  if (props.modelValue) emit('update:modelValue', '')
}
function kivalaszt(it) {
  keres.value = it.nev
  nyitva.value = false
  emit('update:modelValue', it.id)
}
function kesleltetettZaras() { setTimeout(() => { nyitva.value = false }, 150) }

async function mentes() {
  const nev = keres.value.trim()
  if (!nev) return
  hiba.value = ''
  const letezo = props.items.find(x => x.nev.toLowerCase() === nev.toLowerCase())
  if (letezo) { kivalaszt(letezo); return }
  try {
    const res = await client.post(props.postPath, { nev, ...props.extra })
    emit('created', res.data)
    emit('update:modelValue', res.data.id)
    keres.value = res.data.nev
    nyitva.value = false
  } catch (e) {
    hiba.value = e.response?.data?.message || 'Mentés sikertelen.'
  }
}
</script>

<template>
  <div class="gyorskereso">
    <div class="gk-row">
      <input v-model="keres" :placeholder="placeholder" autocomplete="off"
        @focus="nyitva = true" @input="onInput" @blur="kesleltetettZaras" @keydown.enter.prevent="mentes" />
      <button type="button" class="gk-mentes" :disabled="!keres.trim()" @click="mentes">{{ mentesLabel }}</button>
    </div>
    <ul v-if="nyitva && szurtLista.length" class="gk-lista">
      <li v-for="it in szurtLista" :key="it.id" @mousedown.prevent="kivalaszt(it)">{{ it.nev }}</li>
    </ul>
    <p v-if="hiba" class="gk-hiba">{{ hiba }}</p>
  </div>
</template>

<style scoped>
.gyorskereso { position: relative; }
.gk-row { display: flex; gap: 6px; flex-wrap: wrap; }
.gk-row input { flex: 1; min-width: 0; text-transform: none; }
.gk-mentes { white-space: nowrap; font-size: 11px; font-weight: 700; padding: 0 10px; border-radius: 6px; border: 1px solid rgba(43,58,46,.2); background: #fff; color: var(--chalk-green); cursor: pointer; text-transform: none; }
.gk-mentes:disabled { opacity: .4; cursor: not-allowed; }
.gk-lista { position: absolute; z-index: 20; left: 0; right: 0; top: 100%; margin: 2px 0 0; padding: 4px; list-style: none; background: #fff; border: 1px solid rgba(43,58,46,.15); border-radius: 8px; box-shadow: 0 8px 24px rgba(0,0,0,.12); max-height: 190px; overflow-y: auto; }
.gk-lista li { padding: 7px 9px; border-radius: 6px; cursor: pointer; font-size: 13px; font-weight: 500; text-transform: none; color: var(--chalk-green); }
.gk-lista li:hover { background: rgba(107,122,79,.1); }
.gk-hiba { color: #b3441e; font-size: 11px; margin: 3px 0 0; text-transform: none; }
</style>
