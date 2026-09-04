<script setup>
import { ref, onMounted } from 'vue'
import client from '../api/client'

const props = defineProps({
  title: { type: String, required: true },
  apiPath: { type: String, required: true },
  nevKotelezo: { type: Boolean, default: true },
  nevLabel: { type: String, default: 'Név' },
  nevPlaceholder: { type: String, default: '' },
  mutatMegjegyzes: { type: Boolean, default: true }
})
const emit = defineEmits(['close', 'changed'])

const lista = ref([])
const hiba = ref('')
const betolt = ref(true)
const ujNev = ref('')
const ujMegjegyzes = ref('')
const szerkesztId = ref(null)
const szerkNev = ref('')
const szerkMegjegyzes = ref('')

async function frissit() {
  betolt.value = true
  hiba.value = ''
  try {
    lista.value = (await client.get(props.apiPath)).data
  } catch (e) {
    hiba.value = 'Nem sikerült betölteni a listát.'
  } finally {
    betolt.value = false
  }
}
onMounted(frissit)

async function ujFelvitel() {
  if (props.nevKotelezo && !ujNev.value.trim()) { hiba.value = 'A név kötelező.'; return }
  hiba.value = ''
  try {
    await client.post(props.apiPath, { nev: ujNev.value.trim() || null, megjegyzes: ujMegjegyzes.value.trim() || null })
    ujNev.value = ''; ujMegjegyzes.value = ''
    await frissit()
    emit('changed')
  } catch (e) {
    hiba.value = e.response?.data?.message || 'Mentés sikertelen.'
  }
}

function szerkesztesInditasa(item) {
  szerkesztId.value = item.id
  szerkNev.value = item.nev || ''
  szerkMegjegyzes.value = item.megjegyzes || ''
}
function megse() { szerkesztId.value = null }

async function mentSzerkesztes(id) {
  if (props.nevKotelezo && !szerkNev.value.trim()) { hiba.value = 'A név kötelező.'; return }
  hiba.value = ''
  try {
    await client.put(`${props.apiPath}/${id}`, { nev: szerkNev.value.trim() || null, megjegyzes: szerkMegjegyzes.value.trim() || null })
    szerkesztId.value = null
    await frissit()
    emit('changed')
  } catch (e) {
    hiba.value = e.response?.data?.message || 'Mentés sikertelen.'
  }
}

async function torol(item) {
  if (!confirm(`Biztosan törlöd: "${item.nev || '(névtelen)'}"?`)) return
  hiba.value = ''
  try {
    await client.delete(`${props.apiPath}/${item.id}`)
    await frissit()
    emit('changed')
  } catch (e) {
    hiba.value = e.response?.data?.message || 'Törlés sikertelen.'
  }
}
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="modal">
      <div class="modal-head">
        <h3>{{ title }}</h3>
        <button class="close" @click="emit('close')">✕</button>
      </div>

      <p v-if="hiba" class="hiba">{{ hiba }}</p>
      <p v-if="betolt">Betöltés…</p>

      <ul v-else class="lista">
        <li v-for="item in lista" :key="item.id">
          <template v-if="szerkesztId === item.id">
            <input v-model="szerkNev" :placeholder="nevPlaceholder" />
            <input v-if="mutatMegjegyzes" v-model="szerkMegjegyzes" placeholder="Megjegyzés (opcionális)" />
            <button class="icon" title="Mentés" @click="mentSzerkesztes(item.id)">✓</button>
            <button class="icon" title="Mégse" @click="megse">✕</button>
          </template>
          <template v-else>
            <span class="nev">{{ item.nev || '(névtelen)' }}</span>
            <span v-if="item.megjegyzes" class="megj">({{ item.megjegyzes }})</span>
            <button class="icon" title="Szerkesztés" @click="szerkesztesInditasa(item)">✏️</button>
            <button class="icon" title="Törlés" @click="torol(item)">🗑️</button>
          </template>
        </li>
        <li v-if="!lista.length" class="ures">Még nincs felvéve semmi.</li>
      </ul>

      <div class="uj-form">
        <input v-model="ujNev" :placeholder="nevPlaceholder || nevLabel" @keyup.enter="ujFelvitel" />
        <input v-if="mutatMegjegyzes" v-model="ujMegjegyzes" placeholder="Megjegyzés (opcionális)" @keyup.enter="ujFelvitel" />
        <button class="add" @click="ujFelvitel">+ Új</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.overlay { position: fixed; inset: 0; background: rgba(35,38,32,0.5); display: flex; align-items: center; justify-content: center; z-index: 50; padding: 16px; }
.modal { background: #F7F3E8; border-radius: 10px; padding: 18px 20px; width: 100%; max-width: 440px; max-height: 80vh; overflow-y: auto; }
.modal-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; }
.modal-head h3 { margin: 0; font-size: 16px; color: #2B3A2E; }
.close { background: none; border: none; font-size: 16px; cursor: pointer; color: #6B7A4F; }
.hiba { color: #B3441E; font-size: 13px; margin: 4px 0; }
.lista { list-style: none; margin: 0 0 14px; padding: 0; }
.lista li { display: flex; align-items: center; gap: 8px; padding: 8px 4px; border-bottom: 1px solid #D8CFB8; flex-wrap: wrap; }
.lista li.ures { color: #6B7A4F; font-size: 13px; }
.nev { font-weight: 600; }
.megj { font-size: 12px; color: #6B7A4F; }
.icon { margin-left: auto; background: none; border: none; cursor: pointer; font-size: 14px; padding: 2px 4px; }
.lista li input { flex: 1; min-width: 100px; padding: 6px 8px; border: 1px solid #D8CFB8; border-radius: 6px; }
.uj-form { display: flex; gap: 8px; flex-wrap: wrap; }
.uj-form input { flex: 1; min-width: 100px; padding: 8px 10px; border: 1px solid #D8CFB8; border-radius: 6px; }
.add { background: #C1440E; color: #fff; border: none; border-radius: 6px; padding: 8px 14px; font-weight: 600; cursor: pointer; }
@media (max-width: 420px) {
  .uj-form { flex-direction: column; }
  .uj-form input, .add { width: 100%; }
}
</style>
