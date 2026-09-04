import { defineStore } from 'pinia'
import { ref } from 'vue'
import client from '../api/client'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token') || '')
  const felhasznalonev = ref(localStorage.getItem('felhasznalonev') || '')
  const role = ref(localStorage.getItem('role') || '')

  async function login(nev, jelszo) {
    const res = await client.post('/auth/login', { felhasznalonev: nev, jelszo })
    token.value = res.data.token
    felhasznalonev.value = res.data.felhasznalonev
    role.value = res.data.role
    localStorage.setItem('token', token.value)
    localStorage.setItem('felhasznalonev', felhasznalonev.value)
    localStorage.setItem('role', role.value)
  }

  function logout() {
    token.value = ''
    felhasznalonev.value = ''
    role.value = ''
    localStorage.removeItem('token')
    localStorage.removeItem('felhasznalonev')
    localStorage.removeItem('role')
  }

  return { token, felhasznalonev, role, login, logout }
})
