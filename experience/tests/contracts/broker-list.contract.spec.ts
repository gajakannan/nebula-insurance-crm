import { PactV4, MatchersV3 } from '@pact-foundation/pact'
import { describe, it, expect } from 'vitest'
import path from 'path'

const provider = new PactV4({
  consumer: 'nebula-experience',
  provider: 'nebula-api',
  dir: path.resolve(__dirname, '../../pacts'),
})

describe('Broker List Contract', () => {
  it('returns the first page of brokers', async () => {
    await provider
      .addInteraction()
      .given('brokers exist')
      .uponReceiving('a request for the first broker list page')
      .withRequest('GET', '/brokers', (builder) => {
        builder.query({ page: '1', pageSize: '10' })
      })
      .willRespondWith(200, (builder) => {
        builder.jsonBody({
          data: MatchersV3.eachLike({
            id: MatchersV3.uuid(),
            legalName: MatchersV3.string('Acme Brokerage'),
            status: MatchersV3.string('Active'),
          }),
          page: 1,
          pageSize: 10,
          totalCount: MatchersV3.integer(),
          totalPages: MatchersV3.integer(),
        })
      })
      .executeTest(async (mockServer) => {
        const response = await fetch(
          `${mockServer.url}/brokers?page=1&pageSize=10`
        )
        expect(response.status).toBe(200)

        const body = await response.json()
        expect(body.data).toBeDefined()
        expect(body.page).toBe(1)
        expect(body.pageSize).toBe(10)
        expect(body.totalCount).toBeDefined()
        expect(body.totalPages).toBeDefined()
      })
  })
})
