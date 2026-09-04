<script setup>
import { ref, onMounted } from 'vue'
import client from '../api/client'

const emit = defineEmits(['close', 'changed'])

const lista = ref([])
const rekesztipusok = ref([])
const hiba = ref('')
const betolt = ref(true)
const ujNev = ref('')
const ujRekeszTipusId = ref('')
const szerkesztId = ref(null)
const szerkNev = ref('')
const szerkRekeszTipusId = ref('')
const kepFolyamatban = ref(null)
const apiRoot = client.defaults.baseURL.replace(/\/api\/?$/, '')
function kepSrc(u) { return u?.startsWith('http') ? u : apiRoot + u }

async function frissit() {
  betolt.value = true
  hiba.value = ''
  try {
    const [z, r] = await Promise.all([client.get('/zoldsegek'), client.get('/rekesztipusok')])
    lista.value = z.data
    rekesztipusok.value = r.data
  } catch (e) {
    hiba.value = 'Nem sikerült betölteni a listát.'
  } finally {
    betolt.value = false
  }
}
onMounted(frissit)

function rekeszNev(id) {
  return rekesztipusok.value.find(r => r.id === id)?.nev || '—'
}

async function ujFelvitel() {
  if (!ujNev.value.trim()) { hiba.value = 'A név kötelező.'; return }
  hiba.value = ''
  try {
    await client.post('/zoldsegek', { nev: ujNev.value.trim(), alapertelmezettRekeszTipusId: ujRekeszTipusId.value || null })
    ujNev.value = ''; ujRekeszTipusId.value = ''
    await frissit()
    emit('changed')
  } catch (e) {
    hiba.value = e.response?.data?.message || 'Mentés sikertelen.'
  }
}

function szerkesztesInditasa(item) {
  szerkesztId.value = item.id
  szerkNev.value = item.nev
  szerkRekeszTipusId.value = item.alapertelmezettRekeszTipusId || ''
}
function megse() { szerkesztId.value = null }

async function mentSzerkesztes(id) {
  if (!szerkNev.value.trim()) { hiba.value = 'A név kötelező.'; return }
  hiba.value = ''
  try {
    await client.put(`/zoldsegek/${id}`, { nev: szerkNev.value.trim(), alapertelmezettRekeszTipusId: szerkRekeszTipusId.value || null })
    szerkesztId.value = null
    await frissit()
    emit('changed')
  } catch (e) {
    hiba.value = e.response?.data?.message || 'Mentés sikertelen.'
  }
}

async function torol(item) {
  if (!confirm(`Biztosan törlöd: "${item.nev}"?`)) return
  hiba.value = ''
  try {
    await client.delete(`/zoldsegek/${item.id}`)
    await frissit()
    emit('changed')
  } catch (e) {
    hiba.value = e.response?.data?.message || 'Törlés sikertelen.'
  }
}

async function kepValt(item, e) {
  const file = e.target.files[0]
  e.target.value = ''
  if (!file) return
  hiba.value = ''
  kepFolyamatban.value = item.id
  try {
    const fd = new FormData()
    fd.append('kep', file)
    await client.post(`/zoldsegek/${item.id}/kep`, fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    await frissit()
    emit('changed')
  } catch (e2) {
    hiba.value = e2.response?.data?.message || 'Kép feltöltése sikertelen.'
  } finally {
    kepFolyamatban.value = null
  }
}
async function kepTorlese(item) {
  hiba.value = ''
  try {
    await client.delete(`/zoldsegek/${item.id}/kep`)
    await frissit()
    emit('changed')
  } catch (e2) {
    hiba.value = e2.response?.data?.message || 'Kép törlése sikertelen.'
  }
}
</script>

<template>
  <div class="overlay" @click.self="emit('close')">
    <div class="modal">
      <div class="modal-head">
        <h3>Zöldségek</h3>
        <button class="close" @click="emit('close')">✕</button>
      </div>

      <p v-if="hiba" class="hiba">{{ hiba }}</p>
      <p v-if="betolt">Betöltés…</p>

      <ul v-else class="lista">
        <li v-for="item in lista" :key="item.id">
          <template v-if="szerkesztId === item.id">
            <input v-model="szerkNev" placeholder="Zöldség neve" />
            <select v-model="szerkRekeszTipusId">
              <option value="">— nincs alap rekesztípus —</option>
              <option v-for="r in rekesztipusok" :key="r.id" :value="r.id">{{ r.nev }}</option>
            </select>
            <button class="icon" title="Mentés" @click="mentSzerkesztes(item.id)">✓</button>
            <button class="icon" title="Mégse" @click="megse">✕</button>
          </template>
          <template v-else>
            <img v-if="item.kepUrl" :src="kepSrc(item.kepUrl)" class="zoldseg-thumb" alt="" />
            <div v-else class="zoldseg-thumb zoldseg-thumb-empty">🥬</div>
            <span class="nev">{{ item.nev }}</span>
            <span class="megj" v-if="item.alapertelmezettRekeszTipusId">alap: {{ rekeszNev(item.alapertelmezettRekeszTipusId) }}</span>
            <label class="icon kep-upload" title="Kép feltöltése/cseréje">{{ kepFolyamatban===item.id?'⏳':'📷' }}<input type="file" accept="image/*" @change="kepValt(item,$event)" /></label>
            <button v-if="item.kepUrl" class="icon" title="Kép törlése" @click="kepTorlese(item)">🖼️✕</button>
            <button class="icon" title="Szerkesztés" @click="szerkesztesInditasa(item)">✏️</button>
            <button class="icon" title="Törlés" @click="torol(item)">🗑️</button>
          </template>
        </li>
        <li v-if="!lista.length" class="ures">Még nincs felvéve zöldség.</li>
      </ul>

      <div class="uj-form">
        <input v-model="ujNev" placeholder="Zöldség neve" @keyup.enter="ujFelvitel" />
        <select v-model="ujRekeszTipusId">
          <option value="">— nincs alap rekesztípus —</option>
          <option v-for="r in rekesztipusok" :key="r.id" :value="r.id">{{ r.nev }}</option>
        </select>
        <button class="add" @click="ujFelvitel">+ Új</button>
      </div>
      <p class="hint">Az itt megadott rekesztípust ajánlja majd fel a rendszer alapértelmezésként Felvásárláskor és Eladáskor — de tételenként felülírható marad.</p>
    </div>
  </div>
</template>

<style scoped>
.overlay { position: fixed; inset: 0; background: rgba(35,38,32,0.5); display: flex; align-items: center; justify-content: center; z-index: 50; padding: 16px; }
.modal { background: #F7F3E8; border-radius: 10px; padding: 18px 20px; width: 100%; max-width: 460px; max-height: 80vh; overflow-y: auto; }
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
.zoldseg-thumb { width: 34px; height: 34px; border-radius: 8px; object-fit: cover; flex-shrink: 0; }
.zoldseg-thumb-empty { display: flex; align-items: center; justify-content: center; background: rgba(107,122,79,.1); font-size: 15px; }
.kep-upload { position: relative; }
.kep-upload input { position: absolute; inset: 0; opacity: 0; cursor: pointer; width: 100%; }
.lista li input, .lista li select { flex: 1; min-width: 100px; padding: 6px 8px; border: 1px solid #D8CFB8; border-radius: 6px; }
.uj-form { display: flex; gap: 8px; flex-wrap: wrap; }
.uj-form input, .uj-form select { flex: 1; min-width: 100px; padding: 8px 10px; border: 1px solid #D8CFB8; border-radius: 6px; }
.add { background: #C1440E; color: #fff; border: none; border-radius: 6px; padding: 8px 14px; font-weight: 600; cursor: pointer; }
.hint { font-size: 11.5px; color: #6B7A4F; margin: 10px 0 0; }
@media (max-width: 420px) {
  .uj-form { flex-direction: column; }
  .uj-form input, .uj-form select, .add { width: 100%; }
}
</style>
