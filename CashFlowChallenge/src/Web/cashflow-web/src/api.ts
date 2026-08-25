export type MonthlyBalance = {
  year:number; month:number; openingBalance:number; incomeAmount:number; recurringIncomeAmount:number;
  directExpenseAmount:number; recurringExpenseAmount:number; creditCardAmount:number;
  totalIncomeAmount:number; totalExpenseAmount:number; netAmount:number; closingBalance:number; isNegative:boolean;
}
export type Projection = { initialBalance:number; finalBalance:number; hasNegativeMonth:boolean; months:MonthlyBalance[] }

const tokenKey='cashflow_token'
export const session={
  get token(){return localStorage.getItem(tokenKey)},
  set(token:string){localStorage.setItem(tokenKey,token)},
  clear(){localStorage.removeItem(tokenKey)}
}
async function request<T>(url:string, init:RequestInit={}){
  const headers=new Headers(init.headers); headers.set('Content-Type','application/json');
  if(session.token) headers.set('Authorization',`Bearer ${session.token}`)
  const response=await fetch(url,{...init,headers});
  if(response.status===401){session.clear(); throw new Error('Sessão expirada. Entre novamente.')}
  if(!response.ok) throw new Error((await response.text())||`Erro ${response.status}`)
  return response.json() as Promise<T>
}
export async function login(username:string,password:string){
  const data=await request<{token:string;username:string;roles:string[]}>('/auth/login',{method:'POST',body:JSON.stringify({username,password})});
  session.set(data.token); return data
}
export const getMonthly=(year:number,month:number,openingBalance:number)=>request<MonthlyBalance>(`/api/v1/balance/monthly/${year}/${month}?openingBalance=${openingBalance}`)
export const getProjection=(year:number,month:number,months:number,initialBalance:number)=>request<Projection>(`/api/v1/balance/projection?startYear=${year}&startMonth=${month}&months=${months}&initialBalance=${initialBalance}`)
