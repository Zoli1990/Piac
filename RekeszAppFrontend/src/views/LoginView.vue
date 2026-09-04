<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const felhasznalonev = ref('')
const jelszo = ref('')
const hiba = ref('')
const betolt = ref(false)

async function bejelentkezes() {
  hiba.value = ''
  betolt.value = true
  try {
    await auth.login(felhasznalonev.value.trim(), jelszo.value)
    router.push({ name: 'felvasarlas' })
  } catch (e) {
    hiba.value = e.response?.data?.message || 'Hibás felhasználónév vagy jelszó.'
  } finally {
    betolt.value = false
  }
}
</script>

<template>
  <div class="card">
    <h2>Rekesz — bejelentkezés</h2>
    <form @submit.prevent="bejelentkezes">
      <label>Felhasználónév
        <input v-model="felhasznalonev" autofocus autocapitalize="none" autocorrect="off" spellcheck="false" autocomplete="username" />
      </label>
      <label>Jelszó
        <input v-model="jelszo" type="password" autocapitalize="none" autocomplete="current-password" />
      </label>
      <p v-if="hiba" class="hiba">{{ hiba }}</p>
      <button class="btn-primary" type="submit" :disabled="betolt">
        {{ betolt ? 'Bejelentkezés…' : 'Bejelentkezés' }}
      </button>
    </form>
  </div>
</template>

<style scoped>
h2 { margin-top: 0; font-size: 18px; color: var(--chalk-green); }
form { display: flex; flex-direction: column; gap: 12px; }
label { display: flex; flex-direction: column; gap: 5px; font-size: 12.5px; font-weight: 600; color: var(--olive); text-transform: uppercase; letter-spacing: 0.03em; }
button { margin-top: 6px; }
</style>
