export type MonthlyBalance={year:number;month:number;openingBalance:number;incomeAmount:number;recurringIncomeAmount:number;directExpenseAmount:number;recurringExpenseAmount:number;creditCardAmount:number;totalIncomeAmount:number;totalExpenseAmount:number;netAmount:number;closingBalance:number;isNegative:boolean}
export type Projection={initialBalance:number;finalBalance:number;hasNegativeMonth:boolean;months:MonthlyBalance[]}
export type Account={id:string;name:string;type:number;initialBalance:number;isActive:boolean}
export type Category={id:string;name:string;type:number;isActive:boolean}
export type Entry={id:string;amount:number;type:number;description:string;occurredAt:string;isRecurring:boolean;accountId?:string;categoryId?:string}
export type Card={id:string;name:string;limit:number;closingDay:number;dueDay:number;isActive:boolean}
export type Invoice={creditCardId:string;creditCardName:string;year:number;month:number;dueDate:string;totalAmount:number;paidAmount:number;openAmount:number;items:{installmentId:string;description:string;installmentNumber:number;installmentsCount:number;amount:number;isPaid:boolean;categoryId:string}[]}
export type Budget={year:number;month:number;plannedAmount:number;actualAmount:number;remainingAmount:number;isOverBudget:boolean;categories:{categoryId:string;categoryName:string;plannedAmount:number;actualAmount:number;remainingAmount:number;isOverBudget:boolean}[]}
export type Recurring={id:string;amount:number;type:number;description:string;frequency:number;startAt:string;endAt?:string;nextOccurrenceAt:string;isActive:boolean;accountId?:string;categoryId?:string}
const tokenKey='cashflow_token'
const isNative=typeof window!=='undefined'&&(window.location.protocol==='capacitor:'||window.location.hostname==='localhost')
const apiBase=isNative?'https://plania.cloud':''
const endpoint=(url:string)=>`${apiBase}${url}`
export const session={get token(){return localStorage.getItem(tokenKey)},set(t:string){localStorage.setItem(tokenKey,t)},clear(){localStorage.removeItem(tokenKey)}}
async function request<T>(url:string,init:RequestInit={}){const headers=new Headers(init.headers);headers.set('Content-Type','application/json');if(session.token)headers.set('Authorization',`Bearer ${session.token}`);const r=await fetch(endpoint(url),{...init,headers});if(r.status===401){session.clear();throw new Error('Sessão expirada. Entre novamente.')}if(!r.ok)throw new Error((await r.text())||`Erro ${r.status}`);if(r.status===204)return undefined as T;const text=await r.text();return(text?JSON.parse(text):undefined)as T}
export async function login(username:string,password:string){const d=await request<{token:string;username:string;roles:string[]}>('/auth/login',{method:'POST',body:JSON.stringify({username,password})});session.set(d.token);return d}
export const getMonthly=(y:number,m:number,o:number)=>request<MonthlyBalance>(`/api/v1/balance/monthly/${y}/${m}?openingBalance=${o}`)
export const getProjection=(y:number,m:number,n:number,o:number)=>request<Projection>(`/api/v1/balance/projection?startYear=${y}&startMonth=${m}&months=${n}&initialBalance=${o}`)
export const getAccounts=()=>request<Account[]>('/api/v1/accounts')
export const createAccount=(x:{name:string;type:number;initialBalance:number})=>request<Account>('/api/v1/accounts',{method:'POST',body:JSON.stringify(x)})
export const getCategories=()=>request<Category[]>('/api/v1/categories')
export const createCategory=(x:{name:string;type:number})=>request<Category>('/api/v1/categories',{method:'POST',body:JSON.stringify(x)})
export async function getEntries(y:number,m:number){const r=await request<{success:boolean;data:Entry[]}>(`/api/v1/entries/monthly/${y}/${m}`);return r.data??[]}
export const createEntry=(x:unknown)=>request('/api/v1/entries',{method:'POST',body:JSON.stringify(x)})
export const getCards=()=>request<Card[]>('/api/v1/credit-cards')
export const createCard=(x:unknown)=>request<Card>('/api/v1/credit-cards',{method:'POST',body:JSON.stringify(x)})
export const createPurchase=(x:unknown)=>request('/api/v1/credit-cards/purchases',{method:'POST',body:JSON.stringify(x)})
export const getInvoice=(id:string,y:number,m:number)=>request<Invoice>(`/api/v1/credit-cards/${id}/invoices/${y}/${m}`)
export const payInstallment=(id:string)=>request(`/api/v1/credit-cards/installments/${id}/pay`,{method:'POST'})
export const getBudget=(y:number,m:number)=>request<Budget>(`/api/v1/budgets/${y}/${m}`)
export const setBudget=(x:unknown)=>request('/api/v1/budgets',{method:'POST',body:JSON.stringify(x)})
export const getRecurring=()=>request<Recurring[]>('/api/v1/recurring-entries')
export const createRecurring=(x:unknown)=>request('/api/v1/recurring-entries',{method:'POST',body:JSON.stringify(x)})
