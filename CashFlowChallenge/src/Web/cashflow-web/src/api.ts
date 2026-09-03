import { Capacitor, CapacitorHttp } from '@capacitor/core';

export type MonthlyBalance = { year:number; month:number; openingBalance:number; incomeAmount:number; recurringIncomeAmount:number; directExpenseAmount:number; recurringExpenseAmount:number; creditCardAmount:number; plannedExpenseAmount:number; totalIncomeAmount:number; totalExpenseAmount:number; netAmount:number; closingBalance:number; isNegative:boolean };
export type Projection = { initialBalance:number; finalBalance:number; hasNegativeMonth:boolean; totalIncomeAmount?:number; totalExpenseAmount?:number; netAmount?:number; months:MonthlyBalance[] };
export type Account = { id:string; name:string; type:number; initialBalance:number; isActive:boolean };
export type Category = { id:string; name:string; type:number; isActive:boolean };
export type Entry = { id:string; amount:number; type:number; description:string; occurredAt:string; isRecurring:boolean; accountId?:string; categoryId?:string };
export type Card = { id:string; name:string; limit:number; closingDay:number; dueDay:number; isActive:boolean };
export type InvoiceItem = { installmentId:string; purchaseId:string; description:string; purchaseTotalAmount:number; purchaseDate:string; installmentNumber:number; installmentsCount:number; amount:number; isPaid:boolean; categoryId?:string };
export type Invoice = { creditCardId:string; creditCardName:string; year:number; month:number; dueDate:string; totalAmount:number; paidAmount:number; openAmount:number; items:InvoiceItem[] };
export type BudgetCategory = { categoryId:string; categoryName:string; plannedAmount:number; actualAmount:number; remainingAmount:number; isOverBudget:boolean };
export type Budget = { year:number; month:number; plannedAmount:number; actualAmount:number; remainingAmount:number; isOverBudget:boolean; categories:BudgetCategory[] };
export type Recurring = { id:string; amount:number; type:number; description:string; frequency:number; startAt:string; endAt?:string; nextOccurrenceAt:string; isActive:boolean; accountId?:string; categoryId?:string };
export type AuthResult = { token?:string; username?:string; email?:string; requiresGroup:boolean; pendingApproval:boolean; message?:string; group?:{ id:string; name:string; role:string } };
export type GroupInfo = { id:string; name:string; role:string };
export type SessionState = 'active' | 'group' | 'pending';

const tokenKey='cashflow_token';
const sessionStateKey='cashflow_session_state';
const isNative=Capacitor.isNativePlatform();
const apiBase=isNative?'https://plania.cloud':'';
export const session={
  get token(){return localStorage.getItem(tokenKey)},
  get state(){return localStorage.getItem(sessionStateKey) as SessionState|null},
  set(token:string,state:SessionState){localStorage.setItem(tokenKey,token);localStorage.setItem(sessionStateKey,state)},
  clear(){localStorage.removeItem(tokenKey);localStorage.removeItem(sessionStateKey)}
};
const endpoint=(url:string)=>`${apiBase}${url}`;
function buildHeaders(init:RequestInit){const headers:Record<string,string>={'Content-Type':'application/json'};new Headers(init.headers).forEach((value,key)=>{headers[key]=value});if(session.token)headers.Authorization=`Bearer ${session.token}`;return headers}
function parseBody(body:BodyInit|null|undefined){if(typeof body!=='string'||!body)return undefined;try{return JSON.parse(body)}catch{return body}}
function normalizeData<T>(data:unknown){if(typeof data!=='string')return data as T;if(!data)return undefined as T;try{return JSON.parse(data) as T}catch{return data as T}}
function handleUnauthorized(){session.clear();throw new Error('Sessão expirada. Entre novamente.')}
async function request<T>(url:string,init:RequestInit={}){if(isNative){const response=await CapacitorHttp.request({url:endpoint(url),method:init.method??'GET',headers:buildHeaders(init),data:parseBody(init.body)});if(response.status===401)handleUnauthorized();if(response.status<200||response.status>=300){const message=typeof response.data==='string'?response.data:JSON.stringify(response.data);throw new Error(message||`Erro ${response.status}`)}if(response.status===204)return undefined as T;return normalizeData<T>(response.data)}const headers=new Headers(init.headers);headers.set('Content-Type','application/json');if(session.token)headers.set('Authorization',`Bearer ${session.token}`);const response=await fetch(endpoint(url),{...init,headers});if(response.status===401)handleUnauthorized();if(!response.ok)throw new Error((await response.text())||`Erro ${response.status}`);if(response.status===204)return undefined as T;const text=await response.text();return(text?JSON.parse(text):undefined) as T}
function storeAuth(result:AuthResult){if(result.token){const state:SessionState=result.pendingApproval?'pending':result.requiresGroup?'group':'active';session.set(result.token,state)}return result}
export async function login(username:string,password:string){return storeAuth(await request<AuthResult>('/auth/login',{method:'POST',body:JSON.stringify({username,password})}))}
export async function register(username:string,email:string,password:string,groupName:string){return storeAuth(await request<AuthResult>('/auth/register',{method:'POST',body:JSON.stringify({username,email,password,groupName})}))}
export async function googleLogin(idToken:string){return storeAuth(await request<AuthResult>('/auth/google',{method:'POST',body:JSON.stringify({idToken})}))}
export const forgotPassword=(email:string)=>request<{message:string}>('/auth/password/forgot',{method:'POST',body:JSON.stringify({email})});
export const resetPassword=(email:string,token:string,newPassword:string)=>request<void>('/auth/password/reset',{method:'POST',body:JSON.stringify({email,token,newPassword})});
export const changePassword=(currentPassword:string,newPassword:string)=>request<void>('/auth/password/change',{method:'POST',body:JSON.stringify({currentPassword,newPassword})});
export const checkGroup=(name:string)=>request<{exists:boolean;name:string}>(`/auth/groups/check?name=${encodeURIComponent(name)}`);
export async function chooseGroup(groupName:string){return storeAuth(await request<AuthResult>('/auth/group',{method:'POST',body:JSON.stringify({groupName})}))}
export const cancelGroupRequest=()=>request<void>('/auth/group/request',{method:'DELETE'});
export const getGroup=()=>request<GroupInfo>('/auth/group');
export const renameGroup=(groupName:string)=>request<void>('/auth/group',{method:'PUT',body:JSON.stringify({groupName})});
export const getGroupMembers=()=>request<{id:string;email:string;username:string;status:string;role:string}[]>('/auth/group/members');
export const decideGroupMember=(id:string,approve:boolean)=>request<void>(`/auth/group/members/${id}`,{method:'PUT',body:JSON.stringify({approve})});
export const removeGroupMember=(id:string)=>request<void>(`/auth/group/members/${id}`,{method:'DELETE'});
export const getMonthly=(year:number,month:number,openingBalance:number)=>request<MonthlyBalance>(`/api/v1/balance/monthly/${year}/${month}?openingBalance=${openingBalance}`);
export const getProjection=(year:number,month:number,months:number,initialBalance:number)=>request<Projection>(`/api/v1/balance/projection?startYear=${year}&startMonth=${month}&months=${months}&initialBalance=${initialBalance}`);
export const getPlannedProjection=(year:number,month:number,months:number,initialBalance:number)=>request<Projection>(`/api/v1/balance/planned-projection?startYear=${year}&startMonth=${month}&months=${months}&initialBalance=${initialBalance}`);
export const getAccounts=()=>request<Account[]>('/api/v1/accounts');export const createAccount=(body:unknown)=>request<Account>('/api/v1/accounts',{method:'POST',body:JSON.stringify(body)});export const updateAccount=(id:string,body:unknown)=>request<Account>(`/api/v1/accounts/${id}`,{method:'PUT',body:JSON.stringify(body)});export const deleteAccount=(id:string)=>request<void>(`/api/v1/accounts/${id}`,{method:'DELETE'});
export const getCategories=()=>request<Category[]>('/api/v1/categories');export const createCategory=(body:unknown)=>request<Category>('/api/v1/categories',{method:'POST',body:JSON.stringify(body)});export const updateCategory=(id:string,body:unknown)=>request<Category>(`/api/v1/categories/${id}`,{method:'PUT',body:JSON.stringify(body)});export const deleteCategory=(id:string)=>request<void>(`/api/v1/categories/${id}`,{method:'DELETE'});
export async function getEntries(year:number,month:number){const result=await request<{success:boolean;data:Entry[]}>(`/api/v1/entries/monthly/${year}/${month}`);return result.data??[]}
export const createEntry=(body:unknown)=>request<void>('/api/v1/entries',{method:'POST',body:JSON.stringify(body)});export const updateEntry=(id:string,body:unknown)=>request<void>(`/api/v1/entries/${id}`,{method:'PUT',body:JSON.stringify(body)});export const deleteEntry=(id:string)=>request<void>(`/api/v1/entries/${id}`,{method:'DELETE'});
export const getCards=()=>request<Card[]>('/api/v1/credit-cards');export const createCard=(body:unknown)=>request<Card>('/api/v1/credit-cards',{method:'POST',body:JSON.stringify(body)});export const updateCard=(id:string,body:unknown)=>request<Card>(`/api/v1/credit-cards/${id}`,{method:'PUT',body:JSON.stringify(body)});export const deleteCard=(id:string)=>request<void>(`/api/v1/credit-cards/${id}`,{method:'DELETE'});
export const createPurchase=(body:unknown)=>request<void>('/api/v1/credit-cards/purchases',{method:'POST',body:JSON.stringify(body)});export const updatePurchase=(id:string,body:unknown)=>request<void>(`/api/v1/credit-cards/purchases/${id}`,{method:'PUT',body:JSON.stringify(body)});export const deletePurchase=(id:string)=>request<void>(`/api/v1/credit-cards/purchases/${id}`,{method:'DELETE'});export const getInvoice=(id:string,year:number,month:number)=>request<Invoice>(`/api/v1/credit-cards/${id}/invoices/${year}/${month}`);export const payInstallment=(id:string)=>request<void>(`/api/v1/credit-cards/installments/${id}/pay`,{method:'POST'});
export const getBudget=(year:number,month:number)=>request<Budget>(`/api/v1/budgets/${year}/${month}`);export const setBudget=(body:unknown)=>request<void>('/api/v1/budgets',{method:'POST',body:JSON.stringify(body)});export const removeAnnualBudgetCategory=(year:number,categoryId:string)=>request<void>(`/api/v1/budgets/${year}/categories/${categoryId}`,{method:'DELETE'});export const clearAnnualBudget=(year:number)=>request<void>(`/api/v1/budgets/${year}`,{method:'DELETE'});
export const getRecurring=()=>request<Recurring[]>('/api/v1/recurring-entries');export const createRecurring=(body:unknown)=>request<void>('/api/v1/recurring-entries',{method:'POST',body:JSON.stringify(body)});export const updateRecurring=(id:string,body:unknown)=>request<void>(`/api/v1/recurring-entries/${id}`,{method:'PUT',body:JSON.stringify(body)});export const deleteRecurring=(id:string)=>request<void>(`/api/v1/recurring-entries/${id}`,{method:'DELETE'});
